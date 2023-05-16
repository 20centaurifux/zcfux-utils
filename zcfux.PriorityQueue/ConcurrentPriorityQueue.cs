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
using System.Collections;

namespace zcfux.PriorityQueue;

public sealed class ConcurrentPriorityQueue<T> : IPriorityQueue<T>
    where T : IComparable
{
    readonly object _lock = new();
    readonly IPriorityQueue<T> _queue;
    readonly ManualResetEventSlim _ev;

    public ConcurrentPriorityQueue(IPriorityQueue<T> queue)
    {
        _queue = queue;
        _ev = new ManualResetEventSlim(_queue.Count > 0);
    }

    public void Enqueue(T value)
    {
        lock (_lock)
        {
            _queue.Enqueue(value);
        }

        _ev.Set();
    }

    public T? Peek()
    {
        lock (_lock)
        {
            return _queue.Peek();
        }
    }

    public bool TryPeek(out T? value)
    {
        lock (_lock)
        {
            return _queue.TryPeek(out value);
        }
    }

    public T? Pop()
    {
        if (!TryPop(out var value))
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return value;
    }

    public bool TryPop(out T? value)
    {
        bool success;

        lock (_lock)
        {
            success = _queue.TryPop(out value);

            if (_queue.Count == 0)
            {
                _ev.Reset();
            }
        }

        return success;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
            _ev.Reset();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public int RemoveWhere(Predicate<T> pred)
    {
        lock (_lock)
        {
            var count = _queue.RemoveWhere(pred);

            if (_queue.Count == 0)
            {
                _ev.Reset();
            }

            return count;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        T[] values;

        lock (_lock)
        {
            values = _queue.ToArray();
        }

        return values
            .Select(x => x)
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public bool WaitOne(TimeSpan timeout)
        => _ev.WaitHandle.WaitOne(timeout);
}