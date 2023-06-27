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

abstract class ACommandTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    [Repeat(100)]
    public async Task Command()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V1_1>();

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

                // setup api message event handler
                await connection.SubscribeToApiMessagesAsync(
                    new ApiFilter(nodeDetails, "power"), EDirection.In);

                var commandReceivedTcs = new TaskCompletionSource();

                connection.ApiMessageReceivedAsync += e =>
                {
                    if (e.Node.Equals(nodeDetails) &&
                        e is { Api: "power", Topic: "on", Direction: EDirection.In, MessageId: null, ResponseTopic: null })
                    {
                        commandReceivedTcs.SetResult();
                    }

                    return Task.CompletedTask;
                };

                // connect node
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

                        // send command
                        var discoveredNode = await disoveredNodeTcs.Task.WaitAsync(Timeout);

                        var api = discoveredNode.TryGetApi<IPowerApi_V1_1>();

                        await api!.OnAsync().WaitAsync(Timeout);

                        await commandReceivedTcs.Task.WaitAsync(Timeout);
                    }
                }
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task Request()
    {
        var nodeDetails = RandomNodeDetails();

        // connect discoverer
        await using (var connection = CreateConnection())
        {
            await connection
                .ConnectAsync()
                .WaitAsync(Timeout);

            var apiRegistry = new ApiRegistry();

            apiRegistry.Register<IPowerApi_V1_1>();

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

                await discoverer.SetupAsync();

                // connect node
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

                        // send request
                        var discoveredNode = await disoveredNodeTcs.Task.WaitAsync(Timeout);

                        var api = discoveredNode.TryGetApi<IPowerApi_V1_1>();

                        var result = await api!.ToggleAsync().WaitAsync(Timeout);

                        Assert.That(result, Is.True);
                    }
                }
            }
        }
    }
}