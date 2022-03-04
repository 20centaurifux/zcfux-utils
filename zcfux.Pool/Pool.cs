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
namespace zcfux.Pool;

public sealed class Pool<T> : IDisposable
    where T : AResource
{
    readonly IFactory<T> _factory;
    readonly Options _options;
    readonly IKeyBuilder _keyBuilder;
    bool _disposed;

    readonly object _lock = new();
    readonly Dictionary<string, Queue<T>> _resources = new();

    internal Pool(IFactory<T> factory, IKeyBuilder keyBuilder, Options options)
        => (_factory, _keyBuilder, _options) = (factory, keyBuilder, options);

    public T? TryTake(Uri uri)
    {
        T? resource = null;

        var empty = false;

        var key = _keyBuilder.Build(uri);

        while (resource == null && !empty)
        {
            resource = TryDequeue(key);

            if (resource == null)
            {
                empty = true;
            }
            else if (!resource.TryRecycle(uri))
            {
                resource.Free();

                resource = null;
            }
        }

        return resource;
    }

    T? TryDequeue(string key)
    {
        T? resource = null;

        lock (_lock)
        {
            if (_resources.TryGetValue(key, out var queue))
            {
                if (queue.Any())
                {
                    resource = queue.Dequeue();
                }
            }
        }

        return resource;
    }

    public T Create(Uri uri)
    {
        var resource = _factory.Create(uri);

        resource.Suspended += (s, _) =>
        {
            var r = (s as T)!;

            if (!TryEnqueue(r))
            {
                r.Free();
            }
        };

        return resource;
    }

    bool TryEnqueue(T resource)
    {
        var success = false;

        var key = _keyBuilder.Build(resource.Uri);

        lock (_lock)
        {
            if (!_resources.TryGetValue(key, out var queue))
            {
                queue = new Queue<T>();

                _resources[key] = queue;
            }

            if (queue.Count < _options.Limit)
            {
                queue.Enqueue(resource);

                success = true;
            }
        }

        return success;
    }

    public T TakeOrCreate(Uri uri)
        => TryTake(uri)
            ?? Create(uri);

    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            lock (_lock)
            {
                foreach (var (_, queue) in _resources)
                {
                    while (queue.Any())
                    {
                        queue.Dequeue().Free();
                    }
                }
            }
        }

        _disposed = true;
    }
}