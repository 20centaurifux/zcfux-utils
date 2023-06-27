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

namespace zcfux.Telemetry.Test.Discovery;

abstract class ANodeStatusTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    [Test]
    [Repeat(100)]
    public async Task NodeStatusIsOnline()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                // setup event handler
                var tcs = new TaskCompletionSource<IDiscoveredNode>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        if (e2.Status == ENodeStatus.Online)
                        {
                            tcs.SetResult(e1.Node);
                        }

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

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

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // wait for online status
                        var discoveredNode = await tcs.Task.WaitAsync(Timeout);

                        Assert.AreEqual(nodeDetails, discoveredNode.Node);
                        Assert.AreEqual(ENodeStatus.Online, discoveredNode.Status);
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task NodeStatusIsOfflineWhenClientIsShutdownGracefully()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                await connection
                     .ConnectAsync()
                     .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource<IDiscoveredNode>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        if (e2.Status == ENodeStatus.Offline)
                        {
                            tcs.SetResult(e1.Node);
                        }

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

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        await client
                            .ShutdownGracefullyAsync()
                            .WaitAsync(Timeout);

                        // assert node status
                        var discoveredNode = await tcs.Task.WaitAsync(Timeout);

                        Assert.AreEqual(nodeDetails, discoveredNode.Node);
                        Assert.AreEqual(ENodeStatus.Offline, discoveredNode.Status);
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task NodeStatusChangedAsyncReceivesOfflineStatusWhenClientIsShutdownGracefully()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        if (e2.Status == ENodeStatus.Offline)
                        {
                            tcs.SetResult();
                        }

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

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        await client
                            .ShutdownGracefullyAsync()
                            .WaitAsync(Timeout);
                    }

                    // wait for event handler
                    await tcs.Task.WaitAsync(Timeout);
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task NodeStatusIsOfflineWhenConnectionIsClosed()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource<IDiscoveredNode>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        if (e2.Status == ENodeStatus.Offline)
                        {
                            tcs.SetResult(e1.Node);
                        }

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

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);
                    }
                }

                // assert node status
                var discoveredNode = await tcs.Task.WaitAsync(Timeout);

                Assert.AreEqual(nodeDetails, discoveredNode.Node);
                Assert.AreEqual(ENodeStatus.Offline, discoveredNode.Status);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task NodeStatusChangedAsyncReceivesOfflineStatusWhenConnectionIsClosed()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        if (e2.Status == ENodeStatus.Offline)
                        {
                            tcs.SetResult();
                        }

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

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);
                    }
                }

                // wait for event handler
                await tcs.Task.WaitAsync(Timeout);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task NodeStatusChangedAsyncReceivesStatusChanges()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var states = new BlockingCollection<ENodeStatus>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    states.Add(e1.Node.Status);

                    e1.Node.StatusChangedAsync += e2 =>
                    {
                        states.Add(e2.Status);

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new SimpleClient(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        await nodeConnection
                            .ConnectAsync()
                            .WaitAsync(Timeout);

                        // wait for online status
                        for (; ; )
                        {
                            var status = states.Take();

                            if (status == ENodeStatus.Online)
                            {
                                break;
                            }
                        }

                        // change status & assert received event
                        await client
                            .WarningAsync()
                            .WaitAsync(Timeout);

                        Assert.AreEqual(ENodeStatus.Warning, await states
                            .TakeAsync()
                            .WaitAsync(Timeout));

                        await client
                            .ErrorAsync()
                            .WaitAsync(Timeout);

                        Assert.AreEqual(ENodeStatus.Error, await states
                                                .TakeAsync()
                                                .WaitAsync(Timeout));

                        await client
                            .OkAsync()
                            .WaitAsync(Timeout);

                        Assert.AreEqual(ENodeStatus.Online, await states
                            .TakeAsync()
                            .WaitAsync(Timeout));
                    }
                }
            }
        }
    }
}