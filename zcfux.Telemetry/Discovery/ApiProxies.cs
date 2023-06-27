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

sealed class ApiProxies
{
    readonly ApiRegistry _registry;

    readonly object _lock = new();
    readonly HashSet<ApiProxy> _proxies = new();

    public ApiProxies(ApiRegistry registry)
        => _registry = registry;

    public (ApiProxy[] Dropped, ApiProxy[] Registered) Rebuild(ApiInfo[] apis)
    {
        lock (_lock)
        {
            var dropped = FindIncompatibleProxies(apis)
                .ToArray();

            var registered = RegisterMissingProxies(apis)
                .ToArray();

            return (dropped, registered);
        }
    }

    IEnumerable<ApiProxy> FindIncompatibleProxies(ApiInfo[] apis)
        => _proxies.Where(p => !apis.Any(api => p.Api.IsCompatible(api)));

    IEnumerable<ApiProxy> RegisterMissingProxies(ApiInfo[] apis)
    {
        foreach (var api in apis)
        {
            if (!_proxies.Any(p => p.Api.IsCompatible(api)))
            {
                var apiType = _registry.Resolve(api.Topic, api.Version);

                var proxy = new ApiProxy(
                    api,
                    ProxyFactory.CreateProxy(apiType));

                _proxies.Add(proxy);

                yield return proxy;
            }
        }
    }

    public ApiProxy GetProxy(string api)
        => _proxies.Single(p => p.Api.Topic.Equals(api));
}