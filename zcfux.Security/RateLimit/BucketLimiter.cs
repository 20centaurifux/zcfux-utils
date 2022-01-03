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
namespace zcfux.Security.RateLimit;

public sealed class BucketLimiter<TKey> : IRateLimiter<TKey> where TKey : notnull
{
    readonly Dictionary<TKey, Bucket> _buckets = new();
    readonly int _bucketSize;
    readonly int _periodMillis;

    public BucketLimiter(int bucketSize, int periodMillis)
        => (_bucketSize, _periodMillis) = (bucketSize, periodMillis);

    public bool Throttle(TKey key)
    {
        if (!_buckets.TryGetValue(key, out var bucket))
        {
            bucket = new Bucket(_bucketSize, _periodMillis);

            _buckets[key] = bucket;
        }

        var throttled = !bucket.Fill();

        return throttled;
    }
}