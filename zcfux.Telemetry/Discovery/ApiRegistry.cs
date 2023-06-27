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

public sealed class ApiRegistry
{
    sealed record Api(string Topic, int Major, int Minor, Type Type);

    readonly object _lock = new();
    readonly HashSet<Api> _apis = new();

    public void Register<TApi>()
    {
        var t = typeof(TApi);

        var attr = t
            .GetCustomAttributes(typeof(ApiAttribute), false)
            .OfType<ApiAttribute>()
            .Single();

        var (major, minor) = Version.Parse(attr.Version);

        lock (_lock)
        {
            _apis.Add(new Api(attr.Topic, major, minor, t));
        }
    }

    public Type Resolve(string topic, string version)
    {
        var (major, minor) = Version.Parse(version);

        lock (_lock)
        {
            var api = _apis
                .Where(api =>
                    api.Topic.Equals(topic)
                    && (api.Major == major) && (api.Minor <= minor))
                .MinBy(api => api.Minor);

            return api?.Type
                   ?? throw new ApiNotFoundException(topic);
        }
    }
}