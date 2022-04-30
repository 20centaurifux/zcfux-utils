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
using System.Collections.Concurrent;

namespace zcfux.KeyValueStore.Memory;

public sealed class Store : IStore
{
    readonly ConcurrentDictionary<string, byte[]> _m = new();

    public void Setup()
    {
    }
    
    public void Put(string key, Stream stream)
    {
        var ms = new MemoryStream();

        stream.CopyTo(ms);

        _m[key] = ms.ToArray();
    }

    public Stream Fetch(string key)
    {
        if (!_m.TryGetValue(key, out var content))
        {
            throw new KeyNotFoundException(key);
        }

        return new MemoryStream(content);
    }

    public void Remove(string key)
        => _m.Remove(key, out var _);

    public void Dispose()
    {
    }
}