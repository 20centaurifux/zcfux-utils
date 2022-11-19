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

namespace zcfux.CredentialStore;

public sealed class CachingReader : IReader
{
    public sealed record Options(TimeSpan MaxLifetime, TimeSpan ExpiryDateOffset);

    sealed record CachedSecret(Secret Secret)
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public TimeSpan Lifetime => _stopwatch.Elapsed;
    }

    readonly IReader _reader;
    readonly Options _options;
    readonly object _lock = new();
    readonly Dictionary<string, CachedSecret> _m = new();

    public CachingReader(IReader reader, Options options)
        => (_reader, _options) = (reader, options);

    public Secret Read(string path)
    {
        var secret = TryGetCachedSecret_Locked(path);

        if (secret == null)
        {
            secret = _reader.Read(path);

            WriteCachedSecret_Locked(path, secret);
        }

        return secret;
    }

    Secret? TryGetCachedSecret_Locked(string path)
    {
        Secret? secret = null;

        lock (_lock)
        {
            if (_m.TryGetValue(path, out var cachedSecret))
            {
                if (cachedSecret.Lifetime > _options.MaxLifetime
                    || cachedSecret.Secret.IsExpired(Now()))
                {
                    _m.Remove(path);
                }
                else
                {
                    secret = cachedSecret.Secret;
                }
            }
        }

        return secret;
    }

    void WriteCachedSecret_Locked(string path, Secret secret)
    {
        lock (_lock)
        {
            _m[path] = new CachedSecret(secret);
        }
    }

    public void CollectGarbage()
    {
        lock (_lock)
        {
            var now = Now();

            var keysToRemove = _m
                .Where(kv => (kv.Value.Lifetime > _options.MaxLifetime) || kv.Value.Secret.IsExpired(now))
                .Select(kv => kv.Key).ToArray();

            foreach (var key in keysToRemove)
            {
                _m.Remove(key);
            }
        }
    }

    DateTime Now()
        => DateTime.UtcNow.Add(_options.ExpiryDateOffset);
}