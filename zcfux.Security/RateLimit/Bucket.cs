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

namespace zcfux.Security.RateLimit;

internal sealed class Bucket
{
    readonly Queue<long> _bucket;

    readonly int _capacity;
    readonly long _leakRate;

    public Bucket(int capacity, TimeSpan leakRate)
    {
        _bucket = new Queue<long>(capacity);
        _capacity = capacity;
        _leakRate = leakRate.Ticks;
    }

    public bool Fill()
    {
        Leak();

        if (_bucket.Count < _capacity)
        {
            _bucket.Enqueue(Stopwatch.GetTimestamp());

            return true;
        }

        return false;
    }

    void Leak()
    {
        while (_bucket.TryPeek(out var ticks)
               && IsExpired(ticks))
        {
            _bucket.Dequeue();
        }
    }

    bool IsExpired(long ticks)
        => (Stopwatch.GetTimestamp() - ticks) >= _leakRate;
}