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
using System.Collections.Concurrent;

namespace zcfux.Telemetry;

public sealed class Producer<T> : IAsyncEnumerable<T>, IProducer
{
    readonly object _lock = new();
    readonly HashSet<Enumerator> _enumerators = new();
    bool _enabled;
    CancellationTokenSource? _cancellationTokenSource;
    readonly Queue<T> _queuedValues = new();
    
    sealed class Enumerator : IAsyncEnumerator<T>
    {
        public event EventHandler? Disposed;

        readonly BlockingCollection<T> _queue = new();

        readonly object _lock = new();

        readonly CancellationToken _externalCancellationToken;
        readonly CancellationTokenSource _cancellationTokenSource;
        T _value = default!;

        public Enumerator(
            CancellationToken internalCancellationToken,
            CancellationToken externalCancellationToken)
        {
            _externalCancellationToken = externalCancellationToken;

            _cancellationTokenSource = CancellationTokenSource
                .CreateLinkedTokenSource(
                    internalCancellationToken,
                    externalCancellationToken);
        }

        public void Write(T value)
            => _queue.Add(value, _cancellationTokenSource.Token);

        public async ValueTask<bool> MoveNextAsync()
        {
            var success = true;

            if (!_queue.TryTake(out var value))
            {
                try
                {
                    value = await TakeAsync();
                }
                catch (OperationCanceledException)
                {
                    success = false;
                }
            }

            _externalCancellationToken.ThrowIfCancellationRequested();

            if (success)
            {
                lock (_lock)
                {
                    _value = value!;
                }
            }

            return success;
        }

        Task<T> TakeAsync()
        {
            return Task.Run(() =>
            {
                var value = _queue.Take(_cancellationTokenSource.Token);

                return value;
            });
        }

        public T Current
        {
            get
            {
                lock (_lock)
                {
                    return _value;
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            Disposed?.Invoke(this, EventArgs.Empty);

            return ValueTask.CompletedTask;
        }
    }

    public Producer(bool enabled = true)
    {
        if (enabled)
        {
            _enabled = true;
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }

    public void Enable()
    {
        lock (_lock)
        {
            if (!_enabled)
            {
                _enabled = true;
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
    }

    public void Disable()
    {
        lock (_lock)
        {
            if (_enabled)
            {
                _cancellationTokenSource?.Cancel();

                _enumerators.Clear();

                _enabled = false;
            }
        }
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        lock (_lock)
        {
            if (_enabled)
            {
                var enumerator = new Enumerator(_cancellationTokenSource!.Token, cancellationToken);

                enumerator.Disposed += (s, _) =>
                {
                    lock (_lock)
                    {
                        _enumerators.Remove((s as Enumerator)!);
                    }
                };

                _enumerators.Add(enumerator);

                foreach (var value in _queuedValues)
                {
                    enumerator.Write(value);
                }

                _queuedValues.Clear();

                return enumerator;
            }
            else
            {
                return EmptyAsyncEnumerator<T>.Instance;
            }
        }
    }
    
    public void Write(T value)
    {
        lock (_lock)
        {
            if (_enumerators.Any())
            {
                foreach (var e in _enumerators)
                {
                    e.Write(value);
                }
            }
            else
            {
                _queuedValues.Enqueue(value);
            }
        }
    }

    void IProducer.Write(object value)
    {
        if (value is T v)
        {
            Write(v);
        }
        else
        {
            throw new InvalidCastException();
        }
    }
}