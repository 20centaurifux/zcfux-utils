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

sealed class DiscoveredNode : IDiscoveredNode
{
    public event Func<NodeStatusEventArgs, Task>? StatusChangedAsync;
    public event Func<ApiRegistrationEventArgs, Task>? ApiRegisteredAsync;
    public event Func<ApiRegistrationEventArgs, Task>? ApiDroppedAsync;

    public NodeDetails Node { get; }

    long _status;

    readonly object _proxiesLock = new();
    readonly HashSet<ApiProxy> _proxies = new();

    public DiscoveredNode(NodeDetails node)
        => Node = node;

    public ENodeStatus Status
        => (ENodeStatus)Interlocked.Read(ref _status);

    public async Task<ENodeStatus> ChangeStatusAsync(ENodeStatus status)
    {
        var previousStatus = (ENodeStatus)Interlocked.Exchange(ref _status, (long)status);

        if (previousStatus != status && StatusChangedAsync is not null)
        {
            await StatusChangedAsync.Invoke(new NodeStatusEventArgs(Node, status));
        }

        return previousStatus;
    }

    public async Task<bool> RegisterApiAsync(ApiProxy proxy)
    {
        bool registered;

        lock (_proxiesLock)
        {
            registered = _proxies.Add(proxy);
        }

        if (registered && ApiRegisteredAsync is not null)
        {
            await ApiRegisteredAsync.Invoke(new ApiRegistrationEventArgs(proxy));
        }

        return registered;
    }

    public async Task<bool> DropApiAsync(ApiProxy proxy)
    {
        bool dropped;

        lock (_proxiesLock)
        {
            dropped = _proxies.Remove(proxy);
        }

        if (dropped && ApiDroppedAsync is not null)
        {
            await ApiDroppedAsync.Invoke(new ApiRegistrationEventArgs(proxy));
        }

        return dropped;
    }

    public bool HasApi<TApi>()
        where TApi : class
        => TryGetApi<TApi>() is not null;

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
            var proxy = _proxies.SingleOrDefault(p => p.Api.Topic.Equals(attr.Topic));

            if (proxy is not null)
            {
                var (actualMajor, actualMinor) = Version.Parse(proxy.Api.Version);
                var (expectedMajor, expectedMinor) = Version.Parse(attr.Version);

                if (actualMajor == expectedMajor && actualMinor >= expectedMinor)
                {
                    api = proxy.Instance as TApi;
                }
            }
        }

        return api;
    }
}