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
using zcfux.Logging;
using zcfux.Telemetry.Device;

namespace zcfux.Telemetry.Discovery;

public sealed class Discoverer
{
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<DiscoveredEventArgs>? Discovered;

    readonly IConnection _connection;
    readonly IReadOnlyCollection<NodeFilter> _filters;
    readonly ApiRegistry _apiRegistry;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly object _discoveredDevicesLock = new();
    readonly Dictionary<NodeDetails, DiscoveredDevice> _discoveredDevices = new();

    public Discoverer(Options options)
    {
        (_connection, _filters, _apiRegistry, _serializer, _logger) = options;

        _connection.ConnectedAsync += ClientConnectedAsync;
        _connection.DisconnectedAsync += ClientDisconnectedAsync;
        _connection.DeviceStatusReceivedAsync += DeviceStatusReceivedAsync;
        _connection.ApiInfoReceivedAsync += ApiInfoReceivedAsync;
    }

    async Task ClientConnectedAsync(EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') connected.", _connection.ClientId);

        var tasks = _filters
            .Select(f => _connection.SubscribeToDeviceStatusAsync(f))
            .ToList();

        tasks.AddRange(
            _filters.Select(f => _connection.SubscribeToApiInfoAsync(
                new ApiFilter(f, ApiFilter.All))));

        await Task.WhenAll(tasks.ToArray());

        Connected?.Invoke(this, EventArgs.Empty);
    }

    Task ClientDisconnectedAsync(EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') disconnected.", _connection.ClientId);

        Disconnected?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    Task DeviceStatusReceivedAsync(NodeStatusEventArgs e)
    {
        var device = RegisterDeviceIfUnknown(e.Node);

        _logger?.Debug(
            "Discoverer (client=`{0}') received device status (domain=`{1}', kind=`{2}', id=`{3}', status={4}).",
            _connection.ClientId,
            device.Domain,
            device.Kind,
            device.Id,
            e.Status);

        device.Status = e.Status;

        return Task.CompletedTask;
    }

    DiscoveredDevice RegisterDeviceIfUnknown(NodeDetails node)
    {
        var discovered = false;

        DiscoveredDevice? discoveredDevice;

        lock (_discoveredDevicesLock)
        {
            if (!_discoveredDevices.TryGetValue(node, out discoveredDevice))
            {
                _logger?.Debug(
                    "Discoverer (client=`{0}') found new device (domain=`{1}', kind=`{2}', id=`{3}').",
                    _connection.ClientId,
                    node.Domain,
                    node.Kind,
                    node.Id);

                discoveredDevice = new DiscoveredDevice(node);

                _discoveredDevices[node] = discoveredDevice;

                discovered = true;
            }
        }

        if (discovered)
        {
            Discovered?.Invoke(this, new DiscoveredEventArgs(discoveredDevice));
        }

        return discoveredDevice;
    }

    Task ApiInfoReceivedAsync(ApiInfoEventArgs e)
    {
        try
        {
            if (TryGetDiscoveredDevice(e.Node) is { } device)
            {
                if (device.Status != ENodeStatus.Offline
                    && !device.HasApi(e.Api, e.Version))
                {
                    _logger?.Debug(
                        "Registering api `{0}' (version=`{1}', domain=`{2}', kind=`{3}', id={4}).",
                        e.Api,
                        e.Version,
                        e.Node.Domain,
                        e.Node.Kind,
                        e.Node.Id);

                    device.RegisterApi(
                        e.Api,
                        e.Version,
                        () =>
                        {
                            var proxyType = _apiRegistry.Resolve(e.Api, e.Version);

                            var options = new Device.OptionsBuilder()
                                .WithDomain(device.Domain)
                                .WithKind(device.Kind)
                                .WithId(device.Id)
                                .WithConnection(_connection)
                                .WithSerializer(_serializer)
                                .WithLogger(_logger)
                                .Build();

                            var proxy = ProxyFactory.CreateApiProxy(proxyType, options);

                            return proxy;
                        });
                }
            }
            else
            {
                _logger?.Warn(
                    "Couldn't register api `{0}', device (domain=`{1}', kind=`{2}', id={3}) not found.",
                    e.Api,
                    e.Node.Domain,
                    e.Node.Kind,
                    e.Node.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        return Task.CompletedTask;
    }

    DiscoveredDevice? TryGetDiscoveredDevice(NodeDetails node)
    {
        lock (_discoveredDevicesLock)
        {
            return _discoveredDevices.GetValueOrDefault(node);
        }
    }
}