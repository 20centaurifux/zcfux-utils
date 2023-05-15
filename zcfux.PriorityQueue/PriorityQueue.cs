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
namespace zcfux.PriorityQueue;

public sealed class PriorityQueue<T>
    where T : IComparable
{
    readonly SortedSet<Item<T>> _items = new();

    public void Enqueue(T value)
    {
        var item = new Item<T>(value);

        var existingItem = _items.LastOrDefault(v => v.Value.CompareTo(value) == 0);

        if (existingItem is not null)
        {
            item.Version = (existingItem.Version - 1);
        }

        _items.Add(item);
    }

    public T? Peek()
    {
        if (!TryPeek(out var value))
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return value;
    }

    public bool TryPeek(out T? value)
    {
        value = _items.Any()
            ? _items.Last().Value
            : default;

        return _items.Any();
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
        var success = false;
        
        if (_items.Count == 0)
        {
            value = default;
        }
        else
        {
            var item = _items.Last();

            _items.Remove(item);

            value = item.Value;

            success = true;
        }
        
        return success;
    }

    public void Clear()
        => _items.Clear();

    public int Count
        => _items.Count;

    public int RemoveWhere(Predicate<T> pred)
        => _items.RemoveWhere(item => pred(item.Value));
}