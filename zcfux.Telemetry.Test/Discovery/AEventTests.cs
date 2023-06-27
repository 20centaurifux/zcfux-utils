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
using NUnit.Framework;
using zcfux.Telemetry.Discovery;

namespace zcfux.Telemetry.Test.Discovery;

abstract class AEventTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    [Repeat(20)]
    public async Task ReceiveEvent()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V2_0>();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup discoverer event handler
                var disoveredNodeTcs = new TaskCompletionSource<IDiscoveredNode>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        disoveredNodeTcs.SetResult(e1.Node);

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V2_0(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // generate & receive events
                        var discoveredNode = await disoveredNodeTcs.Task.WaitAsync(Timeout);

                        var api = discoveredNode.TryGetApi<IPowerApi_V2_0>();

                        var enumerator = api!.Power.GetAsyncEnumerator();

                        await api.SetStateAsync(true);

                        var success = await enumerator
                            .MoveNextAsync()
                            .AsTask()
                            .WaitAsync(Timeout);

                        Assert.IsTrue(success);
                        Assert.IsTrue(enumerator.Current);

                        await api.SetStateAsync(false);

                        success = await enumerator
                            .MoveNextAsync()
                            .AsTask()
                            .WaitAsync(Timeout);

                        Assert.IsTrue(success);
                        Assert.IsFalse(enumerator.Current);
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(20)]
    public async Task ReceiveEventSentWhenDiscovererWasDisconnected()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer to broker & close connection
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V2_0>();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                await discoverer
                    .SetupAsync();

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                await connection
                    .DisconnectAsync()
                    .WaitAsync(Timeout);
            }

            // connect node & generate event
            await using (var nodeConnection = CreateNodeConnection(nodeDetails))
            {
                await nodeConnection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                await using (var client = new PowerClient_V2_0(nodeOpts))
                {
                    await client
                        .SetupAsync()
                        .WaitAsync(Timeout);

                    await client.Power
                        .SetStateAsync(true)
                        .WaitAsync(Timeout);

                    await client
                        .ShutdownGracefullyAsync()
                        .WaitAsync(Timeout);
                }
            }

            // create & connect disoverer
            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                // setup discoverer event handler
                var disoveredNodeTcs = new TaskCompletionSource<IDiscoveredNode>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        disoveredNodeTcs.SetResult(e1.Node);

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer
                    .SetupAsync();

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // connect node
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V2_0(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // receive event
                        var discoveredNode = await disoveredNodeTcs.Task.WaitAsync(Timeout);

                        var api = discoveredNode.TryGetApi<IPowerApi_V2_0>();

                        var enumerator = api!.Power.GetAsyncEnumerator();

                        var success = await enumerator
                            .MoveNextAsync()
                            .AsTask()
                            .WaitAsync(Timeout);

                        Assert.IsTrue(success);
                        Assert.IsTrue(enumerator.Current);
                    }
                }
            }
        }
    }
}