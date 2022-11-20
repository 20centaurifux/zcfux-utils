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
namespace zcfux.Session;

public sealed class ConcurrentStore : AStore, IDisposable
{
    readonly object _lock = new();
    readonly AStore _store;

    public ConcurrentStore(AStore store)
        => _store = store;

    public void Dispose()
    {
        if (_store is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public override void Init(SessionId sessionId, StoreOptions options)
    {
        base.Init(sessionId, options);

        lock (_lock)
        {
            _store.Init(sessionId, options);
        }
    }

    public override object this[string key]
    {
        get
        {
            lock (_lock)
            {
                return _store[key];
            }
        }

        set
        {
            lock (_lock)
            {
                _store[key] = value;
            }
        }
    }

    public override void Remove(string key)
    {
        lock (_lock)
        {
            _store.Remove(key);
        }
    }

    public override bool Has(string key)
    {
        lock (_lock)
        {
            return _store.Has(key);
        }
    }
}