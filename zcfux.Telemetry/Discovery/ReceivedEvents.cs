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

sealed class ReceivedEvents
{
    public event EventHandler<ReceivedEventArgs>? Received;

    readonly object _lock = new();
    readonly Dictionary<string, ReceivedEventQueue> _queues = new();
    readonly HashSet<string> _subscriptions = new();

    public void Subscribe(NodeDetails node, string api, string topic)
    {
        var key = ToKey(node, api, topic);

        ReceivedEventQueue? queue;

        lock (_lock)
        {
            _subscriptions.Add(key);

            if (!_queues.TryGetValue(key, out queue))
            {
                queue = new ReceivedEventQueue(node, api, topic);

                _queues[key] = queue;
            }
        }

        queue.Activate();

        SendEvents(queue);
    }

    public void Unsubscribe(NodeDetails node, string api)
    {
        lock (_lock)
        {
            var keysToRemove = _queues.Keys
                .Where(k => k.StartsWith($"{node.Domain}/{node.Kind}/{node.Id}/{api}/"))
                .ToArray();

            foreach (var k in keysToRemove)
            {
                _subscriptions.Remove(k);

                if (_queues.TryGetValue(k, out var queue))
                {
                    queue.Deactivate();
                }
            }
        }
    }

    public void Add(NodeDetails node, string api, string topic, byte[] payload, TimeSpan timeToLive)
    {
        var key = ToKey(node, api, topic);

        ReceivedEventQueue? queue;
        var subscribed = false;

        lock (_lock)
        {
            if (!_queues.TryGetValue(key, out queue))
            {
                queue = new ReceivedEventQueue(node, api, topic);

                _queues[key] = queue;
            }

            subscribed = _subscriptions.Contains(key);
        }

        queue.Enqueue(new ReceivedEvent(payload, timeToLive));

        if (subscribed)
        {
            SendEvents(queue);
        }
    }

    void SendEvents(ReceivedEventQueue queue)
    {
        while (queue.TryPop() is { } ev)
        {
            if (!ev.IsExpired)
            {
                Received?.Invoke(
                    this,
                    new ReceivedEventArgs(
                        queue.Node,
                        queue.Api,
                        queue.Topic,
                        ev.Payload));
            }
        }
    }

    static string ToKey(NodeDetails node, string api, string topic)
        => $"{node.Domain}/{node.Kind}/{node.Id}/{api}/{topic}";

    public void DropDanglingQueues(TimeSpan expiryTime)
    {
        lock (_lock)
        {
            var keysToRemove = _queues
                .Where(kv => kv.Value.Dangling > expiryTime)
                .Select(kv => kv.Key)
                .ToArray();

            foreach (var k in keysToRemove)
            {
                _queues.Remove(k);
            }
        }
    }
}