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

namespace zcfux.Session;

public sealed class Manager<TStore> : IManager where TStore : AStore, new()
{
    public event EventHandler<SessionEventArgs>? Created;
    public event EventHandler<ExpiredSessionStateEventArgs>? Expired;
    public event EventHandler<SessionEventArgs>? Removed;

    readonly int _ttlMillis;

    const long Stopped = 0;
    const long Stopping = 1;
    const long Started = 2;

    long _state = Stopped;

    Task? _task;
    CancellationTokenSource? _source;

    readonly ConcurrentDictionary<SessionId, SessionState> _m = new();

    public Manager(TimeSpan sessionTimeout)
        => _ttlMillis = Convert.ToInt32(sessionTimeout.TotalMilliseconds);

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _state, Started, Stopped) == Stopped)
        {
            _source = new CancellationTokenSource();

            _task = Task.Factory.StartNew(() =>
            {
                var token = _source.Token;

                var timer = Task.Delay(0);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (timer.Wait(_ttlMillis, token))
                        {
                            long delayMillis = _ttlMillis;

                            foreach (var (key, state) in _m)
                            {
                                var millisLeft = _ttlMillis - state.ElapsedMillis;

                                if (millisLeft > 0)
                                {
                                    delayMillis = Math.Min(delayMillis, millisLeft);
                                }
                                else
                                {
                                    if (_m.TryRemove(key, out var removedState))
                                    {
                                        Expired?.Invoke(this, new ExpiredSessionStateEventArgs(removedState.ToExpiredSessionState()));

                                        removedState.Dispose();
                                    }

                                    Removed?.Invoke(this, key);
                                }
                            }

                            timer = Task.Delay(TimeSpan.FromMilliseconds(delayMillis));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // session background worker cancelled
                    }
                }
            });
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _state, Stopping, Started) == Started)
        {
            _source?.Cancel();

            _task?.Wait();

            Interlocked.Exchange(ref _state, Stopped);
        }
    }

    public SessionState New()
    {
        var key = SessionId.Random();

        var store = new TStore();

        store.Init(key);

        var state = new SessionState(store, new Activator(ActivateSession));

        _m[key] = state;

        Created?.Invoke(this, key);

        return state;
    }

    void ActivateSession(SessionId key)
    {
        Get(key);
    }

    public SessionState Get(SessionId key)
    {
        if (!_m.TryGetValue(key, out var state))
        {
            throw new SessionNotFoundException();
        }

        if (state.IsExpired(_ttlMillis))
        {
            throw new SessionExpiredException();
        }

        return state;
    }

    public void Remove(SessionId key)
    {
        if (_m.TryRemove(key, out var removedState))
        {
            removedState.Dispose();

            Removed?.Invoke(this, key);
        }
    }
}