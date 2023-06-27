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

namespace zcfux.Telemetry.Test.Node;

abstract class AClientTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Test]
    [Repeat(100)]
    public async Task ClientIsConnecting()
    {
        var nodeDetails = RandomNodeDetails();

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new SimpleClient(opts))
            {
                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                Assert.IsFalse(client.IsOnline);

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                Assert.IsTrue(client.IsOnline);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ClientIsDisconnecting()
    {
        var nodeDetails = RandomNodeDetails();

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new SimpleClient(opts))
            {
                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                Assert.IsFalse(client.IsOnline);

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                Assert.IsTrue(client.IsOnline);

                await connection
                    .DisconnectAsync()
                    .WaitAsync(Timeout);

                Assert.IsFalse(client.IsOnline);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task SetupTriggersConnectionHandler()
    {
        var nodeDetails = RandomNodeDetails();

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            await connection
                .ConnectAsync()
                .WaitAsync(Timeout);

            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new SimpleClient(opts))
            {
                Assert.IsFalse(client.IsOnline);

                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                Assert.IsTrue(client.IsOnline);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ClientSendsOnlineAndOfflineStatus()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        await using (var consumerConnection = CreateConnection())
        {
            await consumerConnection
                .ConnectAsync()
                .WaitAsync(Timeout);

            // setup message handler
            await consumerConnection
                .SubscribeToStatusAsync(new NodeFilter(nodeDetails))
                .WaitAsync(Timeout);

            BlockingCollection<ENodeStatus> states = new();

            consumerConnection.StatusReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails)
                    && !states.IsAddingCompleted)
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
            await using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                await using (var producer = new SimpleClient(opts))
                {
                    await producer
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    await producer
                        .ShutdownGracefullyAsync()
                        .WaitAsync(Timeout);
                }
            }

            // assert received status values
            await states
                .WaitAddingCompletedAsync()
                .WaitAsync(Timeout);

            Assert.AreEqual(ENodeStatus.Connecting, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Online, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Offline, await states
                .TakeAsync()
                .WaitAsync(Timeout));
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ClientChangesAndSendsStatus()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        await using (var consumerConnection = CreateConnection())
        {
            await consumerConnection
                .ConnectAsync()
                .WaitAsync(Timeout);

            // setup message handler
            await consumerConnection
                .SubscribeToStatusAsync(new NodeFilter(nodeDetails))
                .WaitAsync(Timeout);

            BlockingCollection<ENodeStatus> states = new();

            consumerConnection.StatusReceivedAsync += e =>
            {
                if (e.Node.Equals(nodeDetails))
                {
                    if (!states.IsAddingCompleted)
                    {
                        states.Add(e.Status);

                        if (e.Status == ENodeStatus.Offline)
                        {
                            states.CompleteAdding();
                        }
                    }
                }

                return Task.CompletedTask;
            };

            // connect producer
            await using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                await using (var producer = new SimpleClient(opts))
                {
                    await producer
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    await producer
                        .WarningAsync()
                        .WaitAsync(Timeout);

                    await producer
                        .ErrorAsync()
                        .WaitAsync(Timeout);

                    await producer
                        .OkAsync()
                        .WaitAsync(Timeout);

                    await producer
                        .ShutdownGracefullyAsync()
                        .WaitAsync(Timeout);
                }
            }

            // assert received status values
            await states
                .WaitAddingCompletedAsync()
                .WaitAsync(Timeout);

            Assert.AreEqual(ENodeStatus.Connecting, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Online, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Warning, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Error, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Online, await states
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(ENodeStatus.Offline, await states
                .TakeAsync()
                .WaitAsync(Timeout));
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ApiIsConnecting()
    {
        var nodeDetails = RandomNodeDetails();

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new OnlineClient(opts))
            {
                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    await reader
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(Timeout);

                    Assert.IsTrue(reader.Current);
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ApiIsDisconnecting()
    {
        var nodeDetails = RandomNodeDetails();

        await using (var connection = CreateNodeConnection(nodeDetails))
        {
            var opts = CreateNodeOptions(nodeDetails, connection);

            await using (var client = new OnlineClient(opts))
            {
                await client
                    .SetupAsync()
                    .WaitAsync(Timeout);

                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    await reader
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(Timeout);

                    Assert.IsTrue(reader.Current);

                    await connection
                        .DisconnectAsync()
                        .WaitAsync(Timeout);

                    await reader
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(Timeout);

                    Assert.IsFalse(reader.Current);
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task ClientSendsApiInfo()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        await using (var consumerConnection = CreateConnection())
        {
            await consumerConnection
                .ConnectAsync()
                .WaitAsync(Timeout);

            // setup message handler
            await consumerConnection
                .SubscribeToApiInfoAsync(new NodeFilter(nodeDetails))
                .WaitAsync(Timeout);

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
            await using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                await using (var producer = new OnlineClient(opts))
                {
                    await producer
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    // assert received apis
                    var apiInfo = (await tcs.Task.WaitAsync(Timeout)).Single();

                    Assert.AreEqual("online", apiInfo.Topic);
                    Assert.AreEqual("1.0", apiInfo.Version);
                }
            }
        }
    }

    [Test]
    [Repeat(20)]
    public async Task ClientSendsEvents()
    {
        var nodeDetails = RandomNodeDetails();

        // connect consumer
        await using (var consumerConnection = CreateConnection())
        {
            await consumerConnection
                .ConnectAsync()
                .WaitAsync(Timeout);

            // setup message handler
            await consumerConnection
                .SubscribeToApiMessagesAsync(
                    new ApiFilter(new NodeFilter(nodeDetails), "numbers"),
                    EDirection.Out)
                .WaitAsync(Timeout);

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
            await using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                var opts = CreateNodeOptions(nodeDetails, consumerConnection);

                await using (var producer = new NumbersClient(opts))
                {
                    await producer
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    await producerConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    producer.SendNumber(1);
                    producer.SendNumber(17);
                    producer.SendNumber(23);

                    await producer
                        .ShutdownGracefullyAsync()
                        .WaitAsync(Timeout);
                }
            }

            // assert received events
            Assert.AreEqual(1, await numbers
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(17, await numbers
                .TakeAsync()
                .WaitAsync(Timeout));

            Assert.AreEqual(23, await numbers
                .TakeAsync()
                .WaitAsync(Timeout));
        }
    }

    [Test]
    [Repeat(100)]
    public async Task Roundtrip()
    {
        const int messageCount = 5;

        var nodeDetails = RandomNodeDetails();

        // connect consumer
        await using (var consumerConnection = CreateConnection())
        {
            await consumerConnection
                .ConnectAsync()
                .WaitAsync(Timeout);

            // setup response handler
            await consumerConnection
                .SubscribeResponseAsync(nodeDetails)
                .WaitAsync(Timeout);

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
            await using (var producerConnection = CreateNodeConnection(nodeDetails))
            {
                await producerConnection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                var opts = CreateNodeOptions(nodeDetails, producerConnection);

                await using (var producer = new PowerClient_V1_0(opts))
                {
                    await producer
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    // call producer method
                    for (var msgId = 1; msgId <= messageCount; ++msgId)
                    {
                        await consumerConnection
                            .SendApiMessageAsync(
                                new ApiMessage(
                                    nodeDetails,
                                    "power",
                                    "toggle",
                                    MessageOptions.Default,
                                    EDirection.In,
                                    null,
                                    $"r/{consumerConnection.ClientId}/{nodeDetails.Domain}/{nodeDetails.Kind}/{nodeDetails.Id}",
                                    msgId))
                            .WaitAsync(Timeout);
                    }

                    await results
                        .WaitAddingCompletedAsync()
                        .WaitAsync(Timeout);
                }
            }

            // assert received results
            Assert.That(results, Has.Count.EqualTo(messageCount));

            var msgIdsLeft = new HashSet<int>(Enumerable.Range(1, messageCount));

            while (results.TryTake(out var t))
            {
                var (msgId, value) = t;

                Assert.IsTrue(msgIdsLeft.Remove(msgId));
                Assert.AreEqual((msgId % 2) != 0, value);
            }
        }
    }
}