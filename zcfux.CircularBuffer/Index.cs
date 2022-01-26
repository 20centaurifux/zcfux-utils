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
namespace zcfux.CircularBuffer;

internal sealed class Index
{
    readonly int _limit;

    public Index(int limit)
        => _limit = (limit > 0)
            ? limit
            : throw new ArgumentException("Invalid limit.");

    public Index(Index index)
        => (_limit, Value) = (index._limit, index.Value);

    public static Index operator ++(Index obj)
    {
        if (++obj.Value == obj._limit)
        {
            obj.Value = 0;
        }

        return obj;
    }

    public static Index operator --(Index obj)
    {
        if (--obj.Value < 0)
        {
            obj.Value = (obj._limit - 1);
        }

        return obj;
    }

    public void Clear()
        => Value = 0;

    public int Value { get; private set; }
}