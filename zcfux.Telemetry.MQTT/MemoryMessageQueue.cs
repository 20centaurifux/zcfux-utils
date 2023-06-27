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
using System.Diagnostics;
using MQTTnet;

namespace zcfux.Telemetry.MQTT;

public sealed class MemoryMessageQueue : IMessageQueue
{
    readonly BlockingCollection<Task<MqttApplicationMessage>> _queue;
    readonly SemaphoreSlim _semaphore = new(0);

    public MemoryMessageQueue(int limit)
        => _queue = new BlockingCollection<Task<MqttApplicationMessage>>(limit);

    public Task EnqueueAsync(
        MqttApplicationMessage message,
        uint secondsToLive,
        CancellationToken cancellationToken)
    {
        Task task;

        try
        {
            var queuedTask = CreateTask(message, secondsToLive, cancellationToken);

            if (!_queue.TryAdd(queuedTask))
            {
                queuedTask.Dispose();

                throw new InvalidOperationException("Queue limit reached.");
            }

            task = queuedTask;

            _semaphore.Release();
        }
        catch (Exception ex)
        {
            task = Task.FromException(ex);
        }

        return task;
    }

    static Task<MqttApplicationMessage> CreateTask(
        MqttApplicationMessage message,
        uint secondsToLive,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var task = new Task<MqttApplicationMessage>(() =>
        {
            var elapsedSeconds = (uint)Math.Floor(stopwatch.Elapsed.TotalSeconds);

            if (secondsToLive > 0
                && elapsedSeconds > secondsToLive)
            {
                throw new TimeoutException();
            }

            message.MessageExpiryInterval -= elapsedSeconds;

            return message;
        }, cancellationToken);

        return task;
    }

    public async Task<MqttApplicationMessage> DequeueAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await _semaphore.WaitAsync(cancellationToken);

                if (_queue.TryTake(out var task))
                {
                    if (!task.IsCanceled)
                    {
                        task.Start();

                        return await task;
                    }
                }
            }
        }
        catch(OperationCanceledException)
        {
            throw new TaskCanceledException();
        }
    }

    public int Count => _queue.Count;
}