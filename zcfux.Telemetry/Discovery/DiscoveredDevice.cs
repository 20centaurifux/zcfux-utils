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
using zcfux.Telemetry.Device;

namespace zcfux.Telemetry.Discovery;

sealed class DiscoveredDevice : IDiscoveredDevice
{
    sealed record Proxy(object Instance, string Version);

    public event EventHandler<StatusEventArgs>? StatusChanged;
    public event EventHandler<RegistrationEventArgs>? Registered;
    public event EventHandler<RegistrationEventArgs>? Dropped;

    readonly NodeDetails _node;

    long _status;

    readonly object _proxiesLock = new();
    readonly Dictionary<string, Proxy> _proxies = new();

    public string Domain
        => _node.Domain;

    public string Kind
        => _node.Kind;

    public int Id
        => _node.Id;

    public ENodeStatus Status
    {
        get => (ENodeStatus)Interlocked.Read(ref _status);

        internal set
        {
            if (Interlocked.Exchange(ref _status, (long)value) != (long)value)
            {
                StatusChanged?.Invoke(this, new StatusEventArgs(value));
            }
        }
    }

    public DiscoveredDevice(NodeDetails node)
        => _node = node;

    internal void RegisterApi(string topic, string version, Func<object> createProxy)
    {
        Proxy? addedProxy = null;
        Proxy? droppedProxy = null;

        lock (_proxiesLock)
        {
            _proxies.TryGetValue(topic, out var match);

            if (match is null || !match.Version.Equals(version))
            {
                addedProxy = new Proxy(createProxy(), version);

                _proxies[topic] = addedProxy;

                if (match is not null)
                {
                    droppedProxy = match;
                }
            }
        }

        if (droppedProxy is not null)
        {
            Dropped?.Invoke(
                this,
                new RegistrationEventArgs(topic, droppedProxy.Version, droppedProxy.Instance));
            
            (droppedProxy.Instance as IProxy)!.ReleaseProxy();
        }

        if (addedProxy is not null)
        {
            Registered?.Invoke(
                this,
                new RegistrationEventArgs(topic, addedProxy.Version, addedProxy.Instance));
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