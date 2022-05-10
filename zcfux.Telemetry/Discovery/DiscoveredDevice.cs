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
namespace zcfux.Telemetry.Discovery;

sealed class DiscoveredDevice : IDiscoveredDevice
{
    sealed record Proxy(object Instance, string Version);

    public event EventHandler<StatusEventArgs>? StatusChanged;
    public event EventHandler<RegistrationEventArgs>? Registered;
    public event EventHandler<RegistrationEventArgs>? Dropped;

    readonly DeviceDetails _device;

    long _status;

    readonly object _proxiesLock = new();
    readonly Dictionary<string, Proxy> _proxies = new();

    public string Domain
        => _device.Domain;

    public string Kind
        => _device.Kind;

    public int Id
        => _device.Id;

    public EDeviceStatus Status
    {
        get => (EDeviceStatus)Interlocked.Read(ref _status);

        internal set
        {
            if (Interlocked.Exchange(ref _status, (long)value) != (long)value)
            {
                StatusChanged?.Invoke(this, new StatusEventArgs(value));

                if (value == EDeviceStatus.Offline)
                {
                    lock(_proxiesLock)
                    {
                        _proxies.Clear();
                    }
                }
            }
        }
    }

    public DiscoveredDevice(DeviceDetails device)
        => _device = device;

    internal void RegisterApi(string topic, string version, Func<object> createProxy)
    {
        Proxy? match;
        RegistrationEventArgs? registeredEventArgs = null;

        lock (_proxiesLock)
        {
            if (!_proxies.TryGetValue(topic, out match)
                || !match.Version.Equals(version))
            {
                var proxy = createProxy();

                registeredEventArgs = new RegistrationEventArgs(topic, version, proxy);

                _proxies[topic] = new Proxy(proxy, version);
            }
        }

        if (registeredEventArgs is { })
        {
            if (match is { })
            {
                Dropped?.Invoke(this, new RegistrationEventArgs(topic, match.Version, match.Instance));
            }

            Registered?.Invoke(this, registeredEventArgs);
        }
    }

    internal bool HasApi(string topic, string version)
    {
        lock (_proxiesLock)
        {
            return _proxies.TryGetValue(topic, out var proxy)
                   && proxy.Version.Equals(version);
        }
    }

    public bool HasApi<TApi>()
        where TApi : class
        => TryGetApi<TApi>() is { };

    public TApi? TryGetApi<TApi>()
        where TApi : class
    {
        TApi? api = null;

        var t = typeof(TApi);

        var attr = t
            .GetCustomAttributes(typeof(ApiAttribute), false)
            .OfType<ApiAttribute>()
            .Single();

        lock (_proxiesLock)
        {
            if (_proxies.TryGetValue(attr.Topic, out var proxy))
            {
                var (major1, minor1) = Version.Parse(attr.Version);
                var (major2, minor2) = Version.Parse(proxy.Version);

                if (major1 == major2 && minor1 <= minor2)
                {
                    api = proxy.Instance as TApi;
                }
            }
        }

        return api;
    }
}