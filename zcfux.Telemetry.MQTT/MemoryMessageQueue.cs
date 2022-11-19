﻿/***************************************************************************
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
    readonly int _limit;

    public MemoryMessageQueue(int limit)
        => (_queue, _limit) = (new BlockingCollection<Task<MqttApplicationMessage>>(limit), limit);

    public Task EnqueueAsync(
        MqttApplicationMessage message,
        uint secondsToLive,
        CancellationToken cancellationToken)
    {
        Task task;

        if (_queue.Count == _limit)
        {
            task = Task.FromException(new InvalidOperationException("Message queue is full."));
        }
        else
        {
            var queuedTask = CreateTask(message, secondsToLive, cancellationToken);

            _queue.Add(queuedTask, cancellationToken);

            task = queuedTask;
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
                throw new TaskCanceledException();
            }

            message.MessageExpiryInterval -= elapsedSeconds;

            return message;
        }, cancellationToken);

        return task;
    }

    public Task<MqttApplicationMessage> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var task = _queue.Take(cancellationToken);

        task.Start();

        return task;
    }
}