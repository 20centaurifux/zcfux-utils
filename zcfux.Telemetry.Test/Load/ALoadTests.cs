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
using zcfux.Telemetry.Discovery;

namespace zcfux.Telemetry.Test.Load;

abstract class ALoadTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    const int ClientCount = 20;
    const int MessageCount = 1000;

    [Test]
    public async Task Events()
    {
        var publisherTasks = new List<Task>();
        var subscriberTasks = new ConcurrentBag<Task<uint[]>>();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<ITestApi>();

            var tcs = new TaskCompletionSource();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                // setup discoverer event handler
                discoverer.DiscoveredAsync += e =>
                {
                    e.Node.ApiRegisteredAsync += _ =>
                    {
                        subscriberTasks.Add(SubscribeAsync(e.Node));

                        if (subscriberTasks.Count == ClientCount)
                        {
                            tcs.SetResult();
                        }

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer
                    .SetupAsync()
                    .WaitAsync(Timeout);

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // start publisher
                for (var clientId = 1; clientId <= ClientCount; clientId++)
                {
                    publisherTasks.Add(PublishAsync(clientId));
                }

                // wait for subscribers & assert received messages
                await tcs.Task;

                await Task
                    .WhenAll(publisherTasks.Concat(subscriberTasks))
                    .WaitAsync(TimeSpan.FromMinutes(1));

                foreach (var result in subscriberTasks.Select(t => t.Result))
                {
                    Assert.That(result, Has.Length.EqualTo(MessageCount));
                }
            }
        }
    }

    async Task PublishAsync(int clientId)
    {
        var nodeDetails = new NodeDetails("d", "test", clientId);

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            await connection
                .ConnectAsync()
                .WaitAsync(Timeout);

            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new TestClient(opts))
            {
                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                for (var i = 0; i < MessageCount; ++i)
                {
                    client.SendMessage(TestContext.CurrentContext.Random.GetString());

                    await Task.Delay(TimeSpan.FromMilliseconds(50));
                }

                await client
                    .ShutdownGracefullyAsync()
                    .WaitAsync(Timeout);
            }
        }
    }

    static async Task<uint[]> SubscribeAsync(IDiscoveredNode discoveredNode)
    {
        var ids = new HashSet<uint>();

        var api = discoveredNode.TryGetApi<ITestApi>()!;

        var enumerator = api.Messages.GetAsyncEnumerator();

        var completed = false;

        while (!completed)
        {
            var moveNextTask = enumerator
                .MoveNextAsync()
                .AsTask();

            var winner = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(30)));

            if (winner == moveNextTask
                && moveNextTask.Result)
            {
                ids.Add(enumerator.Current.Id);

                completed = (ids.Count == MessageCount);
            }
            else
            {
                completed = true;
            }
        }

        return ids.ToArray();
    }
}