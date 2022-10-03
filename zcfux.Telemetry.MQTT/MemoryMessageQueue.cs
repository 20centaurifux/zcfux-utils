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
using MQTTnet;
using System.Diagnostics;

namespace zcfux.Telemetry.MQTT;

public sealed class MemoryMessageQueue : IMessageQueue
{
    sealed record Item(
        MqttApplicationMessage Message,
        uint SecondsToLive,
        Stopwatch? Stopwatch,
        TaskCompletionSource TaskCompletionSource)
    {
        public bool IsExpired
            => Stopwatch is { } && (Stopwatch.Elapsed.TotalSeconds > SecondsToLive);
    }

    readonly object _lock = new();
    readonly Queue<Item> _items = new();
    readonly AutoResetEvent _event = new(false);
    readonly int _limit;

    public MemoryMessageQueue(int limit)
        => _limit = limit;

    public Task EnqueueAsync(MqttApplicationMessage message, uint secondsToLive)
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            if (_items.Count == _limit)
            {
                taskCompletionSource.SetException(
                    new InvalidOperationException("Nessage queue is full."));
            }
            else
            {
                var item = new Item(
                    message,
                    secondsToLive,
                    (secondsToLive == 0)
                        ? null
                        : Stopwatch.StartNew(),
                    taskCompletionSource);

                _items.Enqueue(item);

                _event.Set();
            }
        }

        return taskCompletionSource.Task;
    }

    public Task<MqttApplicationMessage> PeekAsync(CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<MqttApplicationMessage>();

        Task.Run(() =>
        {
            do
            {
                if (TryPeek() is { } message)
                {
                    taskCompletionSource.SetResult(message);
                }
                else
                {
                    _event.WaitOne(TimeSpan.FromMilliseconds(500));
                }
            } while (!taskCompletionSource.Task.IsCompleted
                     && !cancellationToken.IsCancellationRequested);
        }, cancellationToken);

        return taskCompletionSource.Task;
    }

    MqttApplicationMessage? TryPeek()
    {
        MqttApplicationMessage? message = null;
        
        lock (_lock)
        {
            Shrink_Unlocked();

            if (_items.TryPeek(out var item))
            {
                message = item.Message;
            }
        }

        return message;
    }

    public void Dequeue()
    {
        lock (_lock)
        {
            Dequeue_Unlocked(cancelled: false);
        }
    }

    void Shrink_Unlocked()
    {
        while (_items.TryPeek(out var item)
               && item.IsExpired)
        {
            Dequeue_Unlocked(cancelled: true);
        }
    }

    void Dequeue_Unlocked(bool cancelled)
    {
        var taskCompletionSource = _items.Dequeue().TaskCompletionSource;

        if (cancelled)
        {
            taskCompletionSource.SetCanceled();
        }
        else
        {
            taskCompletionSource.SetResult();
        }
    }
}