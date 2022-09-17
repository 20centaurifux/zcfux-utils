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
namespace zcfux.CredentialStore.Memory;

internal sealed class Storage
{
    readonly object _lock = new();

    readonly Dictionary<string, Secret> _m = new();

    public void CollectGarbage()
    {
        lock (_lock)
        {
            var keysToRemove = _m
                .Where(kv => kv.Value.IsExpired())
                .Select(kv => kv.Key)
                .ToArray();

            foreach (var key in keysToRemove)
            {
                _m.Remove(key);
            }
        }
    }
    
    public void Write(string path, Secret secret)
    {
        lock (_lock)
        {
            _m[path] = secret;
        }
    }

    public Secret? TryGetSecret(string path)
    {
        Secret? secret = null;

        lock (_lock)
        {
            if (_m.TryGetValue(path, out secret))
            {
                if (secret.IsExpired())
                {
                    secret = null;
                }
            }
        }

        return secret;
    }
    
    public bool TryRemove(string path)
    {
        var removed = false;

        lock (_lock)
        {
            removed = _m.Remove(path);
        }

        return removed;
    }
}