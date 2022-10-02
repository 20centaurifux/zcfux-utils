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
    readonly IReadOnlyCollection<DeviceFilter> _filters;
    readonly ApiRegistry _apiRegistry;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly object _discoveredDevicesLock = new();
    readonly Dictionary<DeviceDetails, DiscoveredDevice> _discoveredDevices = new();

    public Discoverer(Options options)
    {
        (_connection, _filters, _apiRegistry, _serializer, _logger) = options;

        _connection.Connected += ClientConnected;
        _connection.Disconnected += ClientDisconnected;
        _connection.DeviceStatusReceived += DeviceStatusReceived;
        _connection.ApiInfoReceived += ApiInfoReceived;
    }

    void ClientConnected(object? sender, EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') connected.", _connection.ClientId);

        var tasks = _filters
            .Select(f => _connection.SubscribeToDeviceStatusAsync(f, CancellationToken.None))
            .ToList();

        tasks.AddRange(
            _filters.Select(f => _connection.SubscribeToApiInfoAsync(
                new ApiFilter(f, ApiFilter.All),
                CancellationToken.None)));

        Task.WaitAll(tasks.ToArray());

        Connected?.Invoke(this, EventArgs.Empty);
    }

    void ClientDisconnected(object? sender, EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') disconnected.", _connection.ClientId);

        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    void DeviceStatusReceived(object? sender, DeviceStatusEventArgs e)
    {
        var device = RegisterDeviceIfUnknown(e.Device);

        _logger?.Debug(
            "Discoverer (client=`{0}') received device status (domain=`{1}', kind=`{2}', id=`{3}', status={4}).",
            _connection.ClientId,
            device.Domain,
            device.Kind,
            device.Id,
            e.Status);

        device.Status = e.Status;
    }

    DiscoveredDevice RegisterDeviceIfUnknown(DeviceDetails device)
    {
        var discovered = false;

        DiscoveredDevice? discoveredDevice;

        lock (_discoveredDevicesLock)
        {
            if (!_discoveredDevices.TryGetValue(device, out discoveredDevice))
            {
                _logger?.Debug(
                    "Discoverer (client=`{0}') found new device (domain=`{1}', kind=`{2}', id=`{3}').",
                    _connection.ClientId,
                    device.Domain,
                    device.Kind,
                    device.Id);

                discoveredDevice = new DiscoveredDevice(device);

                _discoveredDevices[device] = discoveredDevice;

                discovered = true;
            }
        }

        if (discovered)
        {
            Discovered?.Invoke(this, new DiscoveredEventArgs(discoveredDevice));
        }

        return discoveredDevice;
    }

    void ApiInfoReceived(object? sender, ApiInfoEventArgs e)
    {
        try
        {
            if (TryGetDiscoveredDevice(e.Device) is { } device)
            {
                if (!device.HasApi(e.Api, e.Version))
                {
                    _logger?.Debug(
                        "Registering api `{0}' (version=`{1}', domain=`{2}', kind=`{3}', id={4}).",
                        e.Api,
                        e.Version,
                        e.Device.Domain,
                        e.Device.Kind,
                        e.Device.Id);

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
                    e.Device.Domain,
                    e.Device.Kind,
                    e.Device.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    DiscoveredDevice? TryGetDiscoveredDevice(DeviceDetails device)
    {
        lock (_discoveredDevicesLock)
        {
            return _discoveredDevices.GetValueOrDefault(device);
        }
    }
}