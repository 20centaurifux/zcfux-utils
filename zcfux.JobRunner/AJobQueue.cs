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

public abstract class AJobQueue
{
    readonly AutoResetEvent _event = new(false);

    public virtual void Setup()
    {
    }

    internal bool Wait(int millis, CancellationToken token)
    {
        var (timeout, peek) = GetTimeout(millis);

        if (timeout == 0)
        {
            _event.Reset();
        }
        else
        {
            Task.Factory.StartNew(() => _event.WaitOne(millis), CancellationToken.None)
                .Wait(millis, token);
        }

        return !peek || (TryPeek(out var job) && job!.IsDue);
    }

    (int, bool) GetTimeout(int millis)
        => PeekAndGetTimeout(millis).GetValueOrDefault((millis, true));

    (int, bool)? PeekAndGetTimeout(int millis)
    {
        (int, bool)? result = null;

        if (TryPeek(out var job))
        {
            var diff = (job!.NextDue! - DateTime.UtcNow);
            var millisLeft = Convert.ToInt32(diff.Value.TotalMilliseconds);

            if (millisLeft > 0)
            {
                result = (millisLeft < millis)
                    ? (millisLeft, false)
                    : (millis, true);
            }
            else
            {
                result = (0, false);
            }
        }

        return result;
    }

    internal AJob? Pop()
    {
        if (TryPeek(out var job))
        {
            if (job is { NextDue: { } })
            {
                job = (job.NextDue <= DateTime.UtcNow) ? Dequeue() : null;
            }
            else
            {
                job = null;
            }
        }

        return job;
    }

    protected abstract bool TryPeek(out AJob? job);

    protected abstract AJob Dequeue();

    public IJobDetails Create<T>() where T : AJob
        => Schedule<T>(DateTime.UtcNow, Array.Empty<string>());

    public IJobDetails Create<T>(string[] args) where T : AJob
        => Schedule<T>(DateTime.UtcNow, args);

    public IJobDetails Schedule<T>(DateTime nextDue) where T : AJob
        => Schedule<T>(nextDue, Array.Empty<string>());

    public IJobDetails Schedule<T>(DateTime nextDue, string[] args) where T : AJob
    {
        var job = (Activator.CreateInstance(typeof(T)) as AJob)!;

        job.Guid = Guid.NewGuid();
        job.Created = DateTime.Now;
        job.Args = args;
        job.NextDue = nextDue;

        Put(job);

        return job;
    }

    protected void Put<T>(Guid guid, DateTime created, string[] args, DateTime? lastDone, DateTime? nextDue, int errors) where T : AJob
    {
        var job = (Activator.CreateInstance(typeof(T)) as AJob)!;

        job.Guid = guid;
        job.Created = created;
        job.Args = args;
        job.LastDone = lastDone;
        job.NextDue = nextDue;
        job.Errors = errors;

        Put(job);
    }

    internal void Put(AJob job)
    {
        switch (job)
        {
            case { Status: EStatus.Active }:
                Enqueue(job);

                _event.Set();
                break;

            case { Status: EStatus.Done }:
                Done(job);
                break;

            case { Status: EStatus.Aborted }:
                Aborted(job);
                break;
        }
    }

    protected abstract void Enqueue(AJob job);

    protected virtual void Done(AJob job)
    {
    }

    protected virtual void Aborted(AJob job)
    {
    }
}