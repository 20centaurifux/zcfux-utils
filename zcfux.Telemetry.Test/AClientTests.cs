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
using zcfux.Telemetry.Node;

namespace zcfux.Telemetry.Test;

public abstract class AClientTests
{
    sealed class SimpleClient : Client, IConnected, IDisconnected
    {
        const long Offline = 0;
        const long Online = 1;

        long _state = Offline;

        public bool IsOnline
            => Interlocked.Read(ref _state) == Online;

        public SimpleClient(Options options) : base(options)
        {
        }

        public void Connected()
            => Interlocked.Exchange(ref _state, Online);

        public void Disconnected()
            => Interlocked.Exchange(ref _state, Offline);

        public async Task OkAsync()
        {
            ChangeStatus(EStatus.Ok);

            await SendStatusAsync();
        }

        public async Task WarningAsync()
        {
            ChangeStatus(EStatus.Warning);

            await SendStatusAsync();
        }

        public async Task ErrorAsync()
        {
            ChangeStatus(EStatus.Error);

            await SendStatusAsync();
        }
    }

    [Test]
    public async Task ClientIsConnecting()
    {
        var nodeDetails = RandomNodeDetails();

        using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            using (var client = new SimpleClient(opts))
            {
                await client.SetupAsync();

                Assert.IsFalse(client.IsOnline);

                await connection.ConnectAsync();

                Assert.IsTrue(client.IsOnline);
            }
        }
    }

    [Test]
    public async Task ClientIsDisconnecting()
    {
        var nodeDetails = RandomNodeDetails();

        using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            using (var client = new SimpleClient(opts))
            {
                await client.SetupAsync();

                Assert.IsFalse(client.IsOnline);

                await connection.ConnectAsync();

                Assert.IsTrue(client.IsOnline);

                await connection.DisconnectAsync();

                Assert.IsFalse(client.IsOnline);
            }
        }
    }

    [Test]
    public async Task SetupTriggersConnectionHandler()
    {
        var nodeDetails = RandomNodeDetails();

        using (var connection = CreateNodeConnection(nodeDetails))
        {
            await connection.ConnectAsync();

            var opts = CreateNodeOptions(nodeDetails, connection);

            using (var client = new SimpleClient(opts))
            {
                Assert.IsFalse(client.IsOnline);

                await client.SetupAsync();

                Assert.IsTrue(client.IsOnline);
            }
        }
    }

    [Test]
    public async Task ClientSendsOnlineAndOfflineStatus()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        using (var consumerConnection = CreateConnection())
        {
            await consumerConnection.ConnectAsync();

            // setup message handler
            await consumerConnection.SubscribeToStatusAsync(new NodeFilter(nodeDetails));

            BlockingCollection<ENodeStatus> states = new();

            consumerConnection.StatusReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails))
                {
                    states.Add(e.Status);

                    if (e.Status == ENodeStatus.Offline)
                    {
                        states.CompleteAdding();
                    }
                }

                return Task.CompletedTask;
            };

            // connect producer
            using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection.ConnectAsync();

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                using (var producer = new SimpleClient(opts))
                {
                    await producer.SetupAsync();
                    await producer.ShutdownGracefullyAsync();
                }
            }

            // assert received status values
            while (!states.IsAddingCompleted)
            {
                await Task.Delay(500);
            }

            Assert.AreEqual(states.Take(), ENodeStatus.Connecting);
            Assert.AreEqual(states.Take(), ENodeStatus.Online);
            Assert.AreEqual(states.Take(), ENodeStatus.Offline);
        }
    }

    [Test]
    public async Task ClientChangesAndSendsStatus()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        using (var consumerConnection = CreateConnection())
        {
            await consumerConnection.ConnectAsync();

            // setup message handler
            await consumerConnection.SubscribeToStatusAsync(new NodeFilter(nodeDetails));

            BlockingCollection<ENodeStatus> states = new();

            consumerConnection.StatusReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails))
                {
                    states.Add(e.Status);

                    if (e.Status == ENodeStatus.Offline)
                    {
                        states.CompleteAdding();
                    }
                }

                return Task.CompletedTask;
            };

            // connect producer
            using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection.ConnectAsync();

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                using (var producer = new SimpleClient(opts))
                {
                    await producer.SetupAsync();

                    await producer.WarningAsync();
                    await producer.ErrorAsync();
                    await producer.OkAsync();

                    await producer.ShutdownGracefullyAsync();
                }
            }

            // assert received status values
            while (!states.IsAddingCompleted)
            {
                await Task.Delay(500);
            }

            Assert.AreEqual(states.Take(), ENodeStatus.Connecting);
            Assert.AreEqual(states.Take(), ENodeStatus.Online);
            Assert.AreEqual(states.Take(), ENodeStatus.Warning);
            Assert.AreEqual(states.Take(), ENodeStatus.Error);
            Assert.AreEqual(states.Take(), ENodeStatus.Online);
            Assert.AreEqual(states.Take(), ENodeStatus.Offline);
        }
    }

    [Api(Topic = "online", Version = "1.0")]
    interface IOnlineApi
    {
        [Event(Topic = "status")] IAsyncEnumerable<bool> Online { get; }
    }

    sealed class OnlineImpl : IOnlineApi, IConnected, IDisconnected
    {
        readonly Producer<bool> _producer = new();

        public IAsyncEnumerable<bool> Online => _producer;

        public void Connected()
            => _producer.Write(true);

        public void Disconnected()
            => _producer.Write(false);
    }

    sealed class OnlineClient : Client
    {
        public IOnlineApi Api { get; } = new OnlineImpl();

        public OnlineClient(Options options) : base(options)
        {
        }
    }

    [Test]
    public async Task ApiIsConnecting()
    {
        var nodeDetails = RandomNodeDetails();

        using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            using (var client = new OnlineClient(opts))
            {
                await client.SetupAsync();

                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection.ConnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsTrue(reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task ApiIsDisconnecting()
    {
        var nodeDetails = RandomNodeDetails();

        using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            using (var client = new OnlineClient(opts))
            {
                await client.SetupAsync();

                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection.ConnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsTrue(reader.Current);

                    await connection.DisconnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsFalse(reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task ClientSendsApiInfo()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        using (var consumerConnection = CreateConnection())
        {
            await consumerConnection.ConnectAsync();

            // setup message handler
            await consumerConnection.SubscribeToApiInfoAsync(new NodeFilter(nodeDetails));

            var tcs = new TaskCompletionSource<ApiInfo[]>();

            consumerConnection.ApiInfoReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails))
                {
                    tcs.SetResult(e.Apis);
                }

                return Task.CompletedTask;
            };

            // connect producer
            using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection.ConnectAsync();

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                using (var producer = new OnlineClient(opts))
                {
                    await producer.SetupAsync();

                    // assert received apis
                    var apiInfo = (await tcs.Task).Single();

                    Assert.AreEqual("online", apiInfo.Topic);
                    Assert.AreEqual("1.0", apiInfo.Version);
                }
            }
        }
    }

    [Api(Topic = "numbers", Version = "1.0")]
    interface INumbersApi
    {
        [Event(Topic = "n")] IAsyncEnumerable<int> Numbers { get; }
    }

    sealed class NumbersImpl : INumbersApi
    {
        readonly Producer<int> _producer = new();

        public IAsyncEnumerable<int> Numbers => _producer;

        public void SendNumber(int number)
            => _producer.Write(number);
    }

    sealed class NumbersClient : Client
    {
        readonly NumbersImpl _api = new();

        public INumbersApi Api => _api;

        public NumbersClient(Options options) : base(options)
        {
        }

        public void SendNumber(int number)
            => _api.SendNumber(number);
    }

    [Test]
    public async Task ClientSendsEvents()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        using (var consumerConnection = CreateConnection())
        {
            await consumerConnection.ConnectAsync();

            // setup message handler
            await consumerConnection.SubscribeToApiMessagesAsync(
                new ApiFilter(new NodeFilter(nodeDetails), "numbers"),
                EDirection.Out);

            BlockingCollection<int> numbers = new();

            var serializer = CreateSerializer();

            consumerConnection.ApiMessageReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails) && e.Topic.Equals("n"))
                {
                    var number = serializer.Deserialize<int>(e.Payload);

                    numbers.Add(number);
                }

                return Task.CompletedTask;
            };

            // connect producer & send events
            using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                var opts = CreateNodeOptions(nodeDetails, consumerConnection);

                using (var producer = new NumbersClient(opts))
                {
                    await producer.SetupAsync();

                    await producerConnection.ConnectAsync();

                    producer.SendNumber(1);
                    producer.SendNumber(17);
                    producer.SendNumber(23);

                    await producer.ShutdownGracefullyAsync();
                }
            }

            // assert received events
            Assert.AreEqual(1, numbers.Take());
            Assert.AreEqual(17, numbers.Take());
            Assert.AreEqual(23, numbers.Take());
        }
    }

    [Api(Topic = "power", Version = "1.0")]
    interface IPowerApi
    {
        [Command(Topic = "toggle")]
        Task<bool> ToggleAsync();
    }

    sealed class PowerImpl : IPowerApi
    {
        readonly object _lock = new();
        bool _value;

        public Task<bool> ToggleAsync()
        {
            bool newState;

            lock (_lock)
            {
                newState = !_value;
                _value = newState;
            }

            return Task.FromResult(newState);
        }
    }

    sealed class Bulb : Client
    {
        public IPowerApi Power { get; } = new PowerImpl();

        public Bulb(Options options) : base(options)
        {
        }
    }

    [Test]
    public async Task Roundtrip()
    {
        const int messageCount = 5;

        var nodeDetails = RandomNodeDetails();

        // connect consumer
        using (var consumerConnection = CreateConnection())
        {
            await consumerConnection.ConnectAsync();

            // setup response handler
            await consumerConnection.SubscribeResponseAsync(nodeDetails);

            BlockingCollection<int> numbers = new();

            var serializer = CreateSerializer();

            var results = new BlockingCollection<(int MessageId, bool Value)>();

            consumerConnection.ResponseReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails))
                {
                    results.Add((e.MessageId, serializer.Deserialize<bool>(e.Payload)));

                    if (results.Count == messageCount)
                    {
                        results.CompleteAdding();
                    }
                }

                return Task.CompletedTask;
            };

            // connect producer
            using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection.ConnectAsync();

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                using (var producer = new Bulb(opts))
                {
                    await producer.SetupAsync();

                    // call producer method
                    for (var msgId = 1; msgId <= messageCount; ++msgId)
                    {
                        await consumerConnection.SendApiMessageAsync(
                            new ApiMessage(
                                nodeDetails,
                                "power",
                                "toggle",
                                MessageOptions.Default,
                                EDirection.In,
                                null,
                                $"r/{consumerConnection.ClientId}/{nodeDetails.Domain}/{nodeDetails.Kind}/{nodeDetails.Id}",
                                msgId));
                    }

                    while (!results.IsAddingCompleted)
                    {
                        await Task.Delay(500);
                    }
                }
            }

            // assert received results
            Assert.That(results, Has.Count.EqualTo(messageCount));

            var msgIdsLeft = new HashSet<int>(Enumerable.Range(1, messageCount));

            while (results.TryTake(out var t))
            {
                var (msgId, value) = t;

                Assert.IsTrue(msgIdsLeft.Remove(msgId));
                Assert.AreEqual(value, (msgId % 2) != 0);
            }
        }
    }

    static NodeDetails RandomNodeDetails()
        => new(
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.Next());

    Options CreateNodeOptions(NodeDetails node, IConnection connection)
    {
        var opts = new OptionsBuilder()
            .WithConnection(connection)
            .WithNode(node)
            .WithSerializer(CreateSerializer())
            .Build();

        return opts;
    }

    protected abstract IConnection CreateConnection();

    protected abstract IConnection CreateNodeConnection(NodeDetails nodeDetails);

    protected abstract ISerializer CreateSerializer();
}