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
using System.Diagnostics;

namespace zcfux.Session;

public sealed class SessionState : IDisposable
{
    readonly AStore _store;
    readonly Activator _activator;

    readonly object _lock = new();
    readonly Stopwatch _watch = Stopwatch.StartNew();

    internal SessionState(AStore store, Activator activator)
    {
        _store = store;
        _activator = activator;
    }

    public void Dispose()
    {
        try
        {
            if (_store is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception)
        {
            // nothing to see here
        }
    }

    public ExpiredSessionState ToExpiredSessionState()
        => new(_store);

    public SessionKey SessionKey
    {
        get => _store.SessionKey ?? throw new InvalidOperationException();
    }

    public object this[string key]
    {
        get
        {
            KeepAlive();

            return _store[key];
        }

        set
        {
            KeepAlive();

            _store[key] = value;
        }
    }

    public void Remove(string key)
    {
        KeepAlive();

        _store.Remove(key);
    }

    public bool Has(string key)
    {
        KeepAlive();

        return _store.Has(key);
    }

    public void KeepAlive()
    {
        _activator.Activate(_store.SessionKey!);

        lock (_lock)
        {
            _watch.Restart();
        }
    }

    public bool IsExpired(long ttlMillis)
        => ElapsedMillis > ttlMillis;

    public long ElapsedMillis
    {
        get
        {
            lock (_lock)
            {
                return _watch.ElapsedMilliseconds;
            }
        }
    }
}