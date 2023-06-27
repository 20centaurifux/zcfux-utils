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
using System.Collections.Generic;

namespace zcfux.Telemetry;

public sealed class Producer<T> : IAsyncEnumerable<T>, IProducer
{
    readonly object _lock = new();
    readonly HashSet<Enumerator> _enumerators = new();
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly Queue<T> _queuedValues = new();

    sealed class Enumerator : IAsyncEnumerator<T>
    {
        public event EventHandler? Disposed;

        readonly BlockingCollection<T> _queue = new();
        readonly SemaphoreSlim _semaphore = new(0);

        readonly CancellationToken _externalCancellationToken;
        readonly CancellationTokenSource _cancellationTokenSource;
        
        readonly object _valueLock = new();
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
        {
            _queue.Add(value, _cancellationTokenSource.Token);
            _semaphore.Release();
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            var success = true;

            try
            {
                await TakeAsync();
            }
            catch (OperationCanceledException)
            {
                success = false;
            }

            _externalCancellationToken.ThrowIfCancellationRequested();

            return success;
        }

        async Task<T> TakeAsync()
        {
            T? value;

            while (!_queue.TryTake(out value))
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
            }

            lock (_valueLock)
            {
                _value = value;
            }

            return value;
        }

        public T Current
        {
            get
            {
                lock (_valueLock)
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

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        var enumerator = new Enumerator(_cancellationTokenSource.Token, cancellationToken);

        T[] initialValues;

        lock (_lock)
        {
            _enumerators.Add(enumerator);

            initialValues = _queuedValues.ToArray();

            _queuedValues.Clear();
        }

        enumerator.Disposed += (_, _) =>
        {
            lock (_lock)
            {
                _enumerators.Remove(enumerator);
            }
        };

        foreach (var value in initialValues)
        {
            enumerator.Write(value);
        }

        return enumerator;
    }

    public void Write(T value)
    {
        Enumerator[] enumerators;

        lock (_lock)
        {
            enumerators = _enumerators.ToArray();

            if (enumerators.Length == 0)
            {
                _queuedValues.Enqueue(value);
            }
        }

        foreach (var e in enumerators)
        {
            e.Write(value);
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