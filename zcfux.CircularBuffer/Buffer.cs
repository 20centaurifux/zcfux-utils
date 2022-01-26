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

namespace zcfux.CircularBuffer;

public sealed class Buffer<T> : IEnumerable<T> where T : notnull
{
    Index _start;
    Index _end;
    readonly T[] _buffer;
    readonly IDictionary<T, int> _m = new Dictionary<T, int>();

    public Buffer(int size)
    {
        _start = new Index(size);
        _end = new Index(size);
        _buffer = new T[size];
    }

    public int Capacity => _buffer.Length;

    public int Size { get; private set; }

    public bool IsFull => (Size == Capacity);

    public void Clear()
    {
        Size = 0;

        _start.Clear();
        _end.Clear();
        _m.Clear();
    }

    public void Append(T item)
    {
        Remember(item);

        if (IsFull)
        {
            Forget(_buffer[_end.Value]);

            _start++;
        }
        else
        {
            Size++;
        }

        _buffer[_end.Value] = item;

        _end++;
    }

    public T Pop()
    {
        ThrowIfEmpty();

        _end--;
        Size--;

        return Steal(_end.Value);
    }

    public T Dequeue()
    {
        ThrowIfEmpty();

        var index = _start.Value;

        _start++;
        Size--;

        return Steal(index);
    }

    void ThrowIfEmpty()
    {
        if (Size == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }
    }

    public bool Contains(T value)
        => _m.TryGetValue(value, out var count) && (count > 0);

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Size)
            {
                throw new IndexOutOfRangeException();
            }

            index += _start.Value;

            if (index >= Capacity)
            {
                index += -Capacity;
            }

            return _buffer[index];
        }
    }

    void Remember(T value)
    {
        _m.TryGetValue(value, out var count);

        _m[value] = count + 1;
    }

    T Steal(int index)
    {
        var value = _buffer[index];

        Forget(value);

        return value;
    }

    void Forget(T value)
    {
        _m.TryGetValue(value, out var count);

        _m[value] = count - 1;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var distance = GetDistance();

        var start = new Index(_start);

        for (var i = 0; i < distance; i++)
        {
            yield return _buffer[start.Value];

            start++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator() as IEnumerator;

    int GetDistance()
    {
        var distance = Capacity;

        if (!IsFull)
        {
            if (_start.Value <= _end.Value)
            {
                distance = (_end.Value - _start.Value);
            }
            else
            {
                distance -= (_start.Value - _end.Value);
            }
        }

        return distance;
    }
}