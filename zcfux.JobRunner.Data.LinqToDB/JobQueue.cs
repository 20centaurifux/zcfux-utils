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
using LinqToDB;
using LinqToDB.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.JobRunner.Data.LinqToDB;

public sealed class JobQueue : AJobQueue, IJobDb
{
    readonly Engine _engine;
    readonly Options _options;
    readonly ConcurrentDictionary<Guid, AJob> _cache = new();

    public JobQueue(Engine engine, Options options)
        => (_engine, _options) = (engine, options);

    public override void Setup()
    {
        base.Setup();

        using (var t = _engine.NewTransaction())
        {
            t.Db()
                .GetTable<JobRelation>()
                .Where(j => j.Running)
                .Set(j => j.Running, false)
                .Update();

            t.Commit = true;
        }
    }

    public IJobDetails Create<T>(object handle) where T : ARegularJob
        => Schedule<T>(handle, DateTime.UtcNow, Array.Empty<string>());

    public IJobDetails Create<T>(object handle, string[] args) where T : ARegularJob
        => Schedule<T>(handle, DateTime.UtcNow, args);

    public IJobDetails Schedule<T>(object handle, DateTime nextDue) where T : ARegularJob
        => Schedule<T>(handle, nextDue, Array.Empty<string>());

    public IJobDetails Schedule<T>(object handle, DateTime nextDue, string[] args) where T : ARegularJob
    {
        var job = NewRegularJob<T>(nextDue, args);

        var db = (handle as Handle)!.Db();

        Enqueue(db, job);

        Notify();

        return job;
    }

    public IJobDetails CreateCronJob<T>(object handle, string expression) where T : ACronJob
        => CreateCronJob<T>(handle, expression, Array.Empty<string>());

    public IJobDetails CreateCronJob<T>(object handle, string expression, string[] args) where T : ACronJob
    {
        var job = NewCronJob<T>(expression, args);

        var db = (handle as Handle)!.Db();

        Enqueue(db, job);

        Notify();

        return job;
    }

    protected override void Enqueue(AJob job)
    {
        using (var t = _engine.NewTransaction())
        {
            Enqueue(t.Db(), job);

            t.Commit = true;
        }
    }

    void Enqueue(DataConnection db, AJob job)
    {
        var existingJob = db
            .GetTable<JobRelation>()
            .SingleOrDefault(j => j.Guid == job.Guid);

        var cached = false;

        if (_options.FreezeThreshold > TimeSpan.Zero)
        {
            var diff = (DateTime.UtcNow - job.NextDue);

            if (diff < _options.FreezeThreshold)
            {
                _cache[job.Guid] = job;

                cached = true;
            }
        }

        if (!cached)
        {
            job.Freeze();
        }

        if (existingJob == null)
        {
            InsertNew(db, job);
        }
        else
        {
            ReEnqueue(db, job);
        }
    }

    static void InsertNew(DataConnection db, AJob job)
    {
        var jobKindRelation = InsertOrGetJobKind(db, job);

        var jobRelation = new JobRelation(job)
        {
            KindId = jobKindRelation.Id!.Value
        };

        db.Insert(jobRelation);
    }

    static JobKindRelation InsertOrGetJobKind(DataConnection db, AJob job)
    {
        var type = job.GetType();

        var fullName = type.FullName!;
        var assembly = type.Assembly.FullName!;

        var jobKindRelation = db
            .GetTable<JobKindRelation>()
            .FirstOrDefault(k => k.Assembly == assembly && k.FullName == fullName);

        if (jobKindRelation == null)
        {
            jobKindRelation = new JobKindRelation
            {
                Assembly = assembly,
                FullName = fullName
            };

            jobKindRelation.Id = db.InsertWithInt32Identity(jobKindRelation);
        }

        return jobKindRelation;
    }

    void ReEnqueue(DataConnection db, AJob job)
    {
        db.GetTable<JobRelation>()
            .Where(j => j.Guid == job.Guid)
            .Set(j => j.Running, false)
            .Set(j => j.Status, job.Status)
            .Set(j => j.LastDone, job.LastDone)
            .Set(j => j.NextDue, job.NextDue)
            .Set(j => j.Errors, job.Errors)
            .Update();

        if (job.Status != EStatus.Active)
        {
            _cache.Remove(job.Guid, out _);
        }
    }

    protected override bool TryPeek(out AJob? job)
    {
        using (var t = _engine.NewTransaction())
        {
            return TryPeek(t.Db(), out job);
        }
    }

    bool TryPeek(DataConnection db, out AJob? job)
    {
        job = null;

        using (var t = _engine.NewTransaction())
        {
            var jobRelation = t.Db()
                .GetTable<JobRelation>()
                .LoadWith(j => j.Kind)
                .Where(j => !j.Running && j.Status == EStatus.Active)
                .OrderBy(j => j.NextDue)
                .FirstOrDefault();

            if (jobRelation != null
                && !_cache.TryGetValue(jobRelation.Guid, out job))
            {
                job = jobRelation.NewJobInstance();

                job?.Restore(jobRelation);
            }
        }

        return (job != null);
    }

    protected override AJob Dequeue()
    {
        using (var t = _engine.NewTransaction())
        {
            var job = Dequeue(t.Db());

            t.Commit = true;

            return job;
        }
    }

    AJob Dequeue(DataConnection db)
    {
        var jobRelation = db
            .GetTable<JobRelation>()
            .LoadWith(j => j.Kind)
            .Where(j => !j.Running && j.Status == EStatus.Active)
            .OrderBy(j => j.NextDue)
            .First();

        db.GetTable<JobRelation>()
            .Where(j => j.Guid == jobRelation.Guid)
            .Set(j => j.Running, true)
            .Update();

        if (!_cache.TryGetValue(jobRelation.Guid, out var job))
        {
            job = jobRelation.NewJobInstance();

            job!.Restore(jobRelation);
        }

        return job;
    }

    protected override void Done(AJob job)
    {
        base.Done(job);

        using (var t = _engine.NewTransaction())
        {
            ReEnqueue(t.Db(), job);

            t.Commit = true;
        }
    }

    protected override void Aborted(AJob job)
    {
        base.Aborted(job);

        using (var t = _engine.NewTransaction())
        {
            ReEnqueue(t.Db(), job);

            t.Commit = true;
        }
    }

    public IEnumerable<IJobDetails> Query(object handle, Query query)
        => (handle as Handle)!.Db().GetTable<JobViewRelation>().Query(query);

    public void Delete(object handle)
        => (handle as Handle)!.Db()
            .GetTable<JobRelation>()
            .Delete();

    public void Delete(object handle, INode filter)
    {
        var expr = filter.ToExpression<JobViewRelation>();

        var db = (handle as Handle)!.Db();

        var ids = db.GetTable<JobViewRelation>()
            .Where(expr)
            .Select(j => j.Guid);

        db.GetTable<JobRelation>()
            .Where(j => ids.Contains(j.Guid))
            .Delete();
    }
}