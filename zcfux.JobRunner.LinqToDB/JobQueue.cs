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

namespace zcfux.JobRunner.LinqToDB;

public sealed class JobQueue : AJobQueue
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

    protected override void Enqueue(AJob job)
    {
        using (var t = _engine.NewTransaction())
        {
            var existingJob = t.Db()
                .GetTable<JobRelation>()
                .SingleOrDefault(j => j.Guid == job.Guid);

            var diff = (DateTime.UtcNow - job.NextDue);

            if (diff < _options.FreezeThreshold)
            {
                _cache[job.Guid] = job;
            }
            else
            {
                job.Freeze();
            }

            if (existingJob == null)
            {
                Enqueue(t.Db(), job);
            }
            else
            {
                ReEnqueue(t.Db(), job);
            }

            t.Commit = true;
        }
    }

    static void Enqueue(DataConnection db, AJob job)
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
        job = null;

        using (var t = _engine.NewTransaction())
        {
            var jobRelation = t
                .Db()
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
            var jobRelation = t.Db()
                .GetTable<JobRelation>()
                .LoadWith(j => j.Kind)
                .Where(j => !j.Running && j.Status == EStatus.Active)
                .OrderBy(j => j.NextDue)
                .First();

            t.Db()
                .GetTable<JobRelation>()
                .Where(j => j.Guid == jobRelation.Guid)
                .Set(j => j.Running, true)
                .Update();

            t.Commit = true;

            if (!_cache.TryGetValue(jobRelation.Guid, out var job))
            {
                job = jobRelation.NewJobInstance();

                job!.Restore(jobRelation);
            }

            return job;
        }
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

    public IEnumerable<IJobDetails> Query(Query query)
    {
        using (var t = _engine.NewTransaction())
        {
            return t.Db().GetTable<JobViewRelation>().Query(query);
        }
    }

    public void Delete()
    {
        using (var t = _engine.NewTransaction())
        {
            t.Db()
                .GetTable<JobRelation>()
                .Delete();

            t.Commit = true;
        }
    }

    public void Delete(INode filter)
    {
        using (var t = _engine.NewTransaction())
        {
            var expr = filter.ToExpression<JobViewRelation>();

            var ids = t.Db()
                .GetTable<JobViewRelation>()
                .Where(expr)
                .Select(j => j.Guid);

            t.Db()
                .GetTable<JobRelation>()
                .Where(j => ids.Contains(j.Guid))
                .Delete();

            t.Commit = true;
        }
    }
}