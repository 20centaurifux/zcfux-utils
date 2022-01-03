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
    readonly Stopwatch _watch = new();
    long _drops;

    readonly int _bucketSize;
    readonly int _periodMillis;

    public Bucket(int bucketSize, int periodMillis = 60000)
        => (_bucketSize, _periodMillis) = (bucketSize, periodMillis);

    public bool Fill()
    {
        var filled = false;

        if (_watch.IsRunning
            && _watch.ElapsedMilliseconds >= _periodMillis)
        {
            _drops = Math.Max(0, _drops - (_watch.ElapsedMilliseconds / _periodMillis));

            _watch.Restart();
        }

        if (_drops < _bucketSize)
        {
            _drops++;

            filled = true;

            _watch.Restart();
        }

        return filled;
    }
}