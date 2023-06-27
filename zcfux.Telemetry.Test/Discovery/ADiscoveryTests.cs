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
using System.Collections.Concurrent;
using System.Diagnostics;
using zcfux.Telemetry.Discovery;

namespace zcfux.Telemetry.Test.Discovery;

abstract class ADiscoveryTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Test]
    [Repeat(3)]
    public async Task NodeDiscovered()
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

                discoverer.DiscoveredAsync += e =>
                {
                    tcs.SetResult(e.Node);

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
                }

                // assert received node
                var discoveredNode = await tcs.Task.WaitAsync(Timeout);

                Assert.AreEqual(nodeDetails, discoveredNode.Node);
            }
        }
    }

    [Test]
    [Repeat(3)]
    public async Task ApiRegisteredWhenVersionMatches()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V1_0>();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource<ApiProxy>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        tcs.SetResult(e2.Proxy);

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node with power api v1.0
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V1_0(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // assert received proxy
                        var proxy = await tcs.Task.WaitAsync(Timeout);

                        Assert.That(proxy.Api.Topic, Is.EqualTo("power"));
                        Assert.That(proxy.Api.Version, Is.EqualTo("1.0"));
                        Assert.That(proxy.Instance, Is.InstanceOf<IPowerApi_V1_0>());
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(3)]
    public async Task ApiRegisteredWhenTypeIsCompatible()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V1_0>();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var tcs = new TaskCompletionSource<ApiProxy>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        tcs.SetResult(e2.Proxy);

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node with power api v1.1
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V1_1(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // assert received proxy
                        var proxy = await tcs.Task.WaitAsync(Timeout);

                        Assert.That(proxy.Api.Topic, Is.EqualTo("power"));
                        Assert.That(proxy.Api.Version, Is.EqualTo("1.1"));
                        Assert.That(proxy.Instance, Is.InstanceOf<IPowerApi_V1_0>());
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(3)]
    public async Task ApiNotRegisteredWhenVersionIsIncompatible()
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

                // setup event handler
                var tcs = new TaskCompletionSource();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        tcs.SetResult();

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node with power api v1.0
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V1_0(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        // test if proxy hasn't been registered
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        Assert.That(tcs.Task.IsCompleted, Is.False);
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(3)]
    public async Task ChangeToIncompatibleApiVersion()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V1_0>();
            apiRegistry.Register<IPowerApi_V2_0>();

            using (var discoverer = CreateDiscoverer(connection, apiRegistry))
            {
                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                // setup event handler
                var proxies = new BlockingCollection<(bool Registered, ApiProxy Api)>();

                discoverer.DiscoveredAsync += e1 =>
                {
                    e1.Node.ApiRegisteredAsync += e2 =>
                    {
                        proxies.Add((true, e2.Proxy));

                        return Task.CompletedTask;
                    };

                    e1.Node.ApiDroppedAsync += e2 =>
                    {
                        proxies.Add((false, e2.Proxy));

                        return Task.CompletedTask;
                    };

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                // connect node with power api v1.0
                await using (var nodeConnection = CreateNodeConnection(nodeDetails))
                {
                    await nodeConnection
                        .ConnectAsync()
                        .WaitAsync(Timeout);

                    var nodeOpts = CreateNodeOptions(nodeDetails, nodeConnection);

                    await using (var client = new PowerClient_V1_0(nodeOpts))
                    {
                        await client
                            .SetupAsync()
                            .WaitAsync(Timeout);

                        await client
                            .ShutdownGracefullyAsync()
                            .WaitAsync(Timeout);
                    }
                }

                // connect node with power api v2.0
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

                        await client
                            .ShutdownGracefullyAsync()
                            .WaitAsync(Timeout);
                    }
                }

                // wait for registered/dropped proxies
                var watch = Stopwatch.StartNew();

                while (proxies.Count < 3)
                {
                    if (watch.Elapsed > Timeout)
                    {
                        throw new TimeoutException();
                    }

                    await Task.Delay(500);
                }

                // assert received proxy events
                var (registered, proxy) = proxies.Take();

                Assert.Multiple(() =>
                {
                    Assert.That(registered, Is.True);

                    Assert.That(proxy.Api.Topic, Is.EqualTo("power"));
                    Assert.That(proxy.Api.Version, Is.EqualTo("1.0"));
                    Assert.That(proxy.Instance, Is.InstanceOf<IPowerApi_V1_0>());
                });

                (registered, proxy) = proxies.Take();

                Assert.Multiple(() =>
                {
                    Assert.That(registered, Is.False);

                    Assert.That(proxy.Api.Topic, Is.EqualTo("power"));
                    Assert.That(proxy.Api.Version, Is.EqualTo("1.0"));
                    Assert.That(proxy.Instance, Is.InstanceOf<IPowerApi_V1_0>());
                });

                (registered, proxy) = proxies.Take();

                Assert.Multiple(() =>
                {
                    Assert.That(registered, Is.True);

                    Assert.That(proxy.Api.Topic, Is.EqualTo("power"));
                    Assert.That(proxy.Api.Version, Is.EqualTo("2.0"));
                    Assert.That(proxy.Instance, Is.InstanceOf<IPowerApi_V2_0>());
                });
            }
        }
    }
}