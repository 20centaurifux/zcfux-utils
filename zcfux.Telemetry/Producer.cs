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
namespace zcfux.Telemetry;

public sealed class Producer<T> : IAsyncEnumerable<T>, IProducer
{
    readonly object _lock = new ();
    readonly HashSet<Enumerator> _enumerators = new();

    sealed class Enumerator : IAsyncEnumerator<T>
    {
        public event EventHandler? Disposed;

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        readonly object _lock = new();
        readonly Queue<T> _queue = new();
        readonly CancellationToken _cancellationToken;
        T _value = default!;

        public Enumerator(CancellationToken cancellationToken)
            => _cancellationToken = cancellationToken;
        
        public void Publish(T value)
        {
            lock(_lock)
            {
                _queue.Enqueue(value);
                _semaphore.Release();
            }
        }
        
        public async ValueTask<bool> MoveNextAsync()
        {
            await _semaphore.WaitAsync(_cancellationToken);

            lock(_lock)
            {
                _value = _queue.Dequeue();
            }

            return true;
        }

        public T Current => _value;

        public ValueTask DisposeAsync()
        {
            Disposed?.Invoke(this, EventArgs.Empty);

            return ValueTask.CompletedTask;
        }
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
      var enumerator = new Enumerator(cancellationToken);

      enumerator.Disposed += (s, _) =>
      {
          lock (_lock)
          {
              _enumerators.Remove((s as Enumerator)!);
          }
      };

      lock (_lock)
      {
          _enumerators.Add(enumerator);
      }

      return enumerator;
    }

    public void Publish(T value)
    {
        lock (_lock)
        {
            foreach (var e in _enumerators)
            {
                e.Publish(value);
            }
        }
    }
    
    void IProducer.Publish(object value)
    {
        if (value is T v)
        {
            Publish(v);
        }
        else
        {
            throw new InvalidCastException();
        }
    }
}