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

namespace zcfux.PubSub;

public sealed class Channel<TMessage>
    : ISubscribable<TMessage>, IDisposable
{
    public event EventHandler<SubscriptionEventArgs>? Subscribed;
    public event EventHandler<SubscriptionEventArgs>? Unsubscribed;

    readonly ChannelOptions _options = new(MaxConcurrentTasks: 5, TaskBlockingTimeout: TimeSpan.Zero);

    readonly ConcurrentDictionary<Subscriber, Action<TMessage>> _m = new();
    readonly SemaphoreSlim _semaphore;

    bool _disposed;

    public Channel()
        => _semaphore = new SemaphoreSlim(_options.MaxConcurrentTasks);

    public Channel(ChannelOptions options)
    {
        _options = options;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentTasks);
    }

    public void Subscribe(Subscriber subscriber, Action<TMessage> callback)
    {
        _m[subscriber] = callback;

        Subscribed?.Invoke(this, new SubscriptionEventArgs(subscriber));
    }

    public void Unsubscribe(Subscriber subscriber)
    {
        if (_m.TryRemove(subscriber, out var _))
        {
            Unsubscribed?.Invoke(this, new SubscriptionEventArgs(subscriber));
        }
    }

    public IEnumerable<Subscriber> GetSubscribers()
        => _m.Keys;

    public async Task BroadcastAsync(TMessage message)
    {
        var tasks = new List<Task>();

        foreach (var callback in _m.Values)
        {
            Task task;

            if (await WaitOnSemaphore())
            {
                task = Task.Run(() =>
                {
                    try
                    {
                        callback(message);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }
            else
            {
                task = Task.FromException(new TimeoutException());
            }

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async Task SendToAsync(Subscriber subscriber, TMessage message)
    {
        if (_m.TryGetValue(subscriber, out var callback))
        {
            if (await WaitOnSemaphore())
            {
                await Task.Run(() =>
                {
                    try
                    {
                        callback(message);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }

    async Task<bool> WaitOnSemaphore()
    {
        var success = true;

        if (_options.TaskBlockingTimeout == TimeSpan.Zero)
        {
            await _semaphore.WaitAsync();
        }
        else
        {
            success = await _semaphore.WaitAsync(_options.TaskBlockingTimeout);
        }

        return success;
    }

    void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _semaphore.Dispose();

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }
}