/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Collections.Concurrent;
using NUnit.Framework;
using zcfux.Telemetry.Device;
using zcfux.Telemetry.Discovery;
using Options = zcfux.Telemetry.Device.Options;

namespace zcfux.Telemetry.Test;

public abstract class ALoadTests
{
    const int ClientCount = 2;
    const int MessageCount = 1000;

    public sealed record Message(uint Id, string Text, DateTime Timestamp);

    [Api(Topic = "test", Version = "1.0")]
    public interface ITestApi
    {
        [Event(Topic = "msg")] IAsyncEnumerable<Message> Messages { get; }
    }

    sealed class TestApi : ITestApi
    {
        uint _id;

        readonly Producer<Message> _producer = new();

        public IAsyncEnumerable<Message> Messages => _producer;

        public void SendMessage(string test)
            => _producer.Write(new Message(++_id, test, DateTime.UtcNow));
    }

    sealed class TestClient : Client
    {
        public ITestApi Api { get; } = new TestApi();

        public TestClient(Options options) : base(options)
        {
        }

        public void SendMessage(string text)
            => (Api as TestApi)!.SendMessage(text);
    }

    [Test]
    public async Task Events()
    {
        var publisherTasks = new List<Task>();
        var subscriberTasks = new ConcurrentBag<Task<int>>();

        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApi>();

            var opts = new Discovery.OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);


            discoverer.Discovered += (_, e) =>
            {
                e.Device.Registered += (_, _) =>
                {
                    subscriberTasks.Add(Task.Run(() => SubscribeAsync(e.Device)));
                };
            };

            await connection.ConnectAsync();

            for (var clientId = 1; clientId <= ClientCount; clientId++)
            {
                publisherTasks.Add(PublishAsync(clientId));
            }

            await Task.WhenAll(publisherTasks.ToArray());
            await Task.WhenAll(subscriberTasks.ToArray());

            Assert.AreEqual(ClientCount, subscriberTasks.Count);
            
            foreach (var subscriberTask in subscriberTasks)
            {
                Assert.AreEqual(MessageCount, subscriberTask.Result);
            }
        }
    }

    async Task PublishAsync(int clientId)
    {
        var device = new DeviceDetails("d", "test", clientId);

        using (var deviceConnection = CreateDeviceConnection(device))
        {
            var deviceOpts = new Device.OptionsBuilder()
                .WithConnection(deviceConnection)
                .WithDevice(device)
                .WithSerializer(CreateSerializer())
                .Build();

            var client = new TestClient(deviceOpts);

            using (client)
            {
                await deviceConnection.ConnectAsync(CancellationToken.None);

                for (var i = 0; i < MessageCount; ++i)
                {
                    client.SendMessage(TestContext.CurrentContext.Random.GetString());

                    await Task.Delay(5, CancellationToken.None);
                }

                await deviceConnection.DisconnectAsync(CancellationToken.None);
            }
        }
    }

    static async Task<int> SubscribeAsync(IDiscoveredDevice device)
    {
        var ids = new HashSet<uint>();

        var api = device.TryGetApi<ITestApi>()!;

        while (true)
        {
            var enumerator = api.Messages.GetAsyncEnumerator();

            var readFromEnumerator = true;

            while (readFromEnumerator)
            {
                var moveNextTask = enumerator
                    .MoveNextAsync()
                    .AsTask();

                var winner = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(30)));

                if (winner != moveNextTask)
                {
                    return ids.Count;
                }

                if (moveNextTask.Result)
                {
                    ids.Add(enumerator.Current.Id);

                    if (ids.Count == MessageCount)
                    {
                        return MessageCount;
                    }
                }
                else
                {
                    readFromEnumerator = false;
                }
            }
        }
    }

    protected abstract IConnection CreateConnection();

    protected abstract IConnection CreateDeviceConnection(DeviceDetails device);

    protected abstract ISerializer CreateSerializer();
}