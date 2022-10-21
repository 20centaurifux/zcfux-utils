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
namespace zcfux.JobRunner;

public sealed class Runner
{
    public event EventHandler<JobEventArgs>? Run;
    public event EventHandler<JobEventArgs>? Done;
    public event EventHandler<FailedJobEventArgs>? Failed;
    public event EventHandler<JobEventArgs>? Aborted;
    public event ErrorEventHandler? Error;

    readonly AJobQueue _queue;
    readonly int _maxErrors;
    readonly int _retrySecs;
    readonly SemaphoreSlim _semaphore;

    const long Stopped = 0;
    const long Starting = 1;
    const long Started = 2;
    const long Stopping = 3;

    long _state = Stopped;

    CancellationTokenSource _cancellationTokenSource = default!;
    CancellationToken _token;
    Task _task = default!;

    long _runningJobs;

    public Runner(AJobQueue queue, Options opts)
    {
        _queue = queue;

        (var maxJobs, _maxErrors, _retrySecs) = opts;

        _semaphore = new SemaphoreSlim(maxJobs);
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _state, Stopped, Starting) == Stopped)
        {
            Interlocked.Exchange(ref _runningJobs, 0);

            _cancellationTokenSource = new CancellationTokenSource();

            _token = _cancellationTokenSource.Token;

            _task = Task.Factory.StartNew(() =>
            {
                Interlocked.Exchange(ref _state, Started);

                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        if (_queue.Wait(5000, _token))
                        {
                            RunNextJob();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(this, new ErrorEventArgs(ex));
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _state, Started, Stopping) == Started)
        {
            _cancellationTokenSource.Cancel();

            _task.Wait(CancellationToken.None);

            WaitForActiveTasks();

            Interlocked.Exchange(ref _state, Stopped);
        }
    }

    void WaitForActiveTasks()
    {
        var pending = Interlocked.Read(ref _runningJobs);

        while (pending != 0)
        {
            Thread.Sleep(500);

            pending = Interlocked.Read(ref _runningJobs);
        }
    }

    void RunNextJob()
    {
        if (_queue.Pop() is { } job)
        {
            _semaphore.Wait(CancellationToken.None);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Run?.Invoke(this, job);

                    job.Run();
                    job.Done();

                    Done?.Invoke(this, job);
                }
                catch (Exception ex)
                {
                    if (job.Errors <= _maxErrors)
                    {
                        job.Fail(_retrySecs);

                        Failed?.Invoke(this, new FailedJobEventArgs(job, ex));
                    }
                    else
                    {
                        job.Abort();

                        Aborted?.Invoke(this, job);
                    }
                }

                try
                {
                    _queue.Put(job);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, new ErrorEventArgs(ex));
                }

                Interlocked.Decrement(ref _runningJobs);

                _semaphore.Release();
            }, CancellationToken.None);

            Interlocked.Increment(ref _runningJobs);
        }
    }
}