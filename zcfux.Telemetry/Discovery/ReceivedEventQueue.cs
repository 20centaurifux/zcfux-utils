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

namespace zcfux.Telemetry.Discovery;

sealed class ReceivedEventQueue
{
    readonly object _lock = new();
    readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    readonly Queue<ReceivedEvent> _events = new();

    public ReceivedEventQueue(NodeDetails node, string api, string topic)
        => (Node, Api, Topic) = (node, api, topic);

    public NodeDetails Node { get; }

    public string Api { get; }

    public string Topic { get; }

    public void Activate()
    {
        lock (_lock)
        {
            _stopwatch.Stop();
        }
    }

    public void Deactivate()
    {
        lock (_lock)
        {
            _stopwatch.Restart();
        }
    }

    public TimeSpan Dangling
    {
        get
        {
            lock (_lock)
            {
                return _stopwatch.IsRunning
                    ? _stopwatch.Elapsed
                    : TimeSpan.Zero;
            }
        }
    }

    public void Enqueue(ReceivedEvent ev)
    {
        if (!ev.IsExpired)
        {
            lock (_lock)
            {
                _events.Enqueue(ev);
            }
        }
    }

    public ReceivedEvent? TryPop()
    {
        ReceivedEvent? ev = null;

        lock (_lock)
        {
            if (!_stopwatch.IsRunning)
            {
                while (ev is null && _events.TryDequeue(out ev))
                {
                    if (ev.IsExpired)
                    {
                        ev = null;
                    }
                }
            }
        }

        return ev;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _events.Count;
            }
        }
    }
}