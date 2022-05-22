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
using NUnit.Framework;
using zcfux.Data;
using zcfux.Filter;
using zcfux.JobRunner.Data;

namespace zcfux.JobRunner.Test;

public abstract class AJobDbTests
{
    IEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();

        _engine.Setup();

        DeleteJobs();
    }

    protected abstract IEngine CreateAndSetupEngine();

    void DeleteJobs()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            jobDb.Delete(t.Handle);

            t.Commit = true;
        }
    }

    protected abstract AJobQueue CreateQueue();

    protected IJobDb CreateJobDb()
        => (CreateQueue() as IJobDb)!;

    [Test]
    public void QueueIsPersistent()
    {
        // Create queue & insert job.
        var jobDb = CreateJobDb();

        Guid guid;

        using (var t = _engine.NewTransaction())
        {
            guid = jobDb.Create<Jobs.Simple>(t.Handle).Guid;

            t.Commit = true;
        }

        // Start runner with a new queue & wait for job.
        var queue = CreateQueue();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        Assert.AreEqual(guid, source.Task.Result);

        runner.Stop();
    }

    [Test]
    public void Create()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.Create<Jobs.Simple>(t.Handle);

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void CreateWithArgs()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.Create<Jobs.Simple>(t.Handle, new[] { "hello", "world" });

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void Schedule()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.Schedule<Jobs.Simple>(t.Handle, DateTime.UtcNow.AddHours(1));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void ScheduleWithArgs()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.Schedule<Jobs.Simple>(
                t.Handle,
                DateTime.UtcNow.AddHours(1),
                new[] { "hello", "world" });

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void CreateCronJob()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.CreateCronJob<Jobs.Cron>(t.Handle, "*/1 * * * * *");

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void CreateCronJobWithArgs()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var created = jobDb.CreateCronJob<Jobs.Cron>(
                t.Handle,
                "*/1 * * * * *",
                new[] { "hello", "world" });

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(created, fetched);
        }
    }

    [Test]
    public void Query_Guid()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var first = jobDb.Create<Jobs.Simple>(t.Handle);

            jobDb.Create<Jobs.Simple>(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(Filters.Guid.EqualTo(first.Guid));

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(first, fetched);
        }
    }

    [Test]
    public void Query_Type()
    {
        var jobDb = CreateJobDb();

        using (var t = _engine.NewTransaction())
        {
            var first = jobDb.Create<Jobs.Simple>(t.Handle);

            jobDb.Create<Jobs.Fail>(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(Filters.Type.EqualTo(typeof(Jobs.Simple).FullName!));

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(first, fetched);
        }
    }

    [Test]
    public void Query_Created()
    {
        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();

            var first = jobDb.Create<Jobs.Simple>(t.Handle);

            Thread.Sleep(1000);

            var second = jobDb.Create<Jobs.Simple>(t.Handle);

            var diff = Convert.ToInt32((second.Created - first.Created).TotalSeconds);

            Assert.Greater(diff, 0);

            var qb = new QueryBuilder()
                .WithFilter(Filters.Created.EqualTo(first.Created));

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(first, fetched);
        }
    }

    [Test]
    public void Query_Status()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        var active = queue.Create<Jobs.Simple>();

        var qb = new QueryBuilder()
            .WithFilter(Filters.Status.EqualTo(EStatus.Active));

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(active, fetched);
        }
    }

    [Test]
    public void Query_Errors()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Fail>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Failed += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        var active = queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithFilter(Filters.Errors.EqualTo(0));

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(active, fetched);
        }
    }

    [Test]
    public void Query_NextDue()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var second = queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithFilter(Filters.NextDue.GreaterThan(DateTime.UtcNow));

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(second, fetched);
        }
    }

    [Test]
    public void Query_LastDone()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        var active = queue.Create<Jobs.Simple>();


        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithFilter(Filters.LastDone.IsNull());

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(active, fetched);
        }
    }

    [Test]
    public void OrderBy_Guid()
    {
        var queue = CreateQueue();

        var jobs = new[]
        {
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>()
        }.OrderBy(job => job.Guid).ToArray();

        var qb = new QueryBuilder()
            .WithOrderBy("Guid");

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            CompareJobs(jobs[0], fetched[0]);
            CompareJobs(jobs[1], fetched[1]);
            CompareJobs(jobs[2], fetched[2]);
        }
    }

    [Test]
    public void OrderBy_Type()
    {
        var queue = CreateQueue();

        var jobs = new[]
        {
            queue.Create<Jobs.Fail>(),
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Twice>()
        }.OrderBy(job => job.GetType().FullName).ToArray();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Type");

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            CompareJobs(jobs[0], fetched[0]);
            CompareJobs(jobs[1], fetched[1]);
            CompareJobs(jobs[2], fetched[2]);
        }
    }

    [Test]
    public void OrderBy_Created()
    {
        var queue = CreateQueue();

        var jobs = new IJobDetails[3];

        for (var i = 0; i < jobs.Length; ++i)
        {
            jobs[i] = queue.Create<Jobs.Simple>();

            if (i < (jobs.Length - 1))
            {
                Thread.Sleep(1000);
            }
        }

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Created");

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            CompareJobs(jobs[0], fetched[0]);
            CompareJobs(jobs[1], fetched[1]);
            CompareJobs(jobs[2], fetched[2]);
        }
    }

    [Test]
    public void OrderBy_Status()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Status");

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            Assert.Less(fetched[0].Status, fetched[1].Status);

            qb = new QueryBuilder()
                .WithOrderByDescending("Status");

            fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            Assert.Less(fetched[1].Status, fetched[0].Status);
        }
    }

    [Test]
    public void OrderBy_Errors()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Fail>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Failed += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Errors");

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            Assert.Less(fetched[0].Errors, fetched[1].Errors);
        }
    }

    [Test]
    public void OrderBy_NextDue()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        var second = queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderByDescending("NextDue");

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            CompareJobs(first, fetched[1]);
            CompareJobs(second, fetched[0]);
        }
    }

    [Test]
    public void OrderBy_LastDone()
    {
        var queue = CreateQueue();

        Guid RunJob()
        {
            queue.Create<Jobs.Simple>();

            var runner = new Runner(queue, new(MaxJobs: 1, MaxErrors: 2, RetrySecs: 1));

            var source = new TaskCompletionSource<Guid>();

            runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

            runner.Start();

            source.Task.Wait();

            runner.Stop();

            return source.Task.Result;
        }

        var first = RunJob();

        Thread.Sleep(1000);

        var second = RunJob();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderByDescending(Filters.LastDone);

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(first, fetched[1].Guid);
            Assert.AreEqual(second, fetched[0].Guid);
        }
    }

    [Test]
    public void Skip()
    {
        var queue = CreateQueue();

        var jobs = new[]
        {
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>()
        }.OrderBy(job => job.Guid).ToArray();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Guid")
                .WithSkip(2);

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(fetched, jobs.Last());
        }
    }

    [Test]
    public void Limit()
    {
        var queue = CreateQueue();

        var jobs = new[]
        {
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>()
        }.OrderBy(job => job.Guid).ToArray();

        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Guid")
                .WithLimit(1);

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(fetched, jobs.First());
        }
    }

    [Test]
    public void SkipAndLimit()
    {
        var queue = CreateQueue();

        var jobs = new[]
        {
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>(),
            queue.Create<Jobs.Simple>()
        }.OrderBy(job => job.Guid).ToArray();


        using (var t = _engine.NewTransaction())
        {
            var qb = new QueryBuilder()
                .WithOrderBy("Guid")
                .WithSkip(1)
                .WithLimit(1);

            var jobDb = CreateJobDb();

            var fetched = jobDb.Query(t.Handle, qb.Build()).Single();

            CompareJobs(fetched, jobs[1]);
        }
    }

    [Test]
    public void Delete()
    {
        var queue = CreateQueue();

        using (var t = _engine.NewTransaction())
        {
            queue.Create<Jobs.Simple>();
            queue.Create<Jobs.Simple>();
            
            var jobDb = CreateJobDb();

            jobDb.Delete(t.Handle);

            Assert.IsEmpty(jobDb.Query(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void Delete_Guid()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();
        var second = queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.Guid.EqualTo(first.Guid));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(second, fetched);
        }
    }

    [Test]
    public void Delete_Type()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Fail>();

        var second = queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.Type.EqualTo(typeof(Jobs.Fail).FullName!));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(second, fetched);
        }
    }

    [Test]
    public void Delete_Created()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        Thread.Sleep(1000);

        var second = queue.Create<Jobs.Simple>();

        var diff = Convert.ToInt32((second.Created - first.Created).TotalSeconds);

        Assert.Greater(diff, 0);

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.Created.EqualTo(first.Created));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(second, fetched);
        }
    }

    [Test]
    public void Delete_Status()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        using (var t = _engine.NewTransaction())
        {
            var active = queue.Create<Jobs.Simple>();
            
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.Status.EqualTo(EStatus.Done));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(active, fetched);
        }
    }

    [Test]
    public void Delete_Errors()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Fail>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Failed += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        var active = queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.Errors.GreaterThan(0));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(active, fetched);
        }
    }

    [Test]
    public void Delete_NextDue()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Filters.NextDue.GreaterThan(DateTime.UtcNow));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(first, fetched);
        }
    }

    [Test]
    public void Delete_LastDone()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var runner = new Runner(queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        source.Task.Wait();

        runner.Stop();

        var active = queue.Create<Jobs.Simple>();

        using (var t = _engine.NewTransaction())
        {
            var jobDb = CreateJobDb();
            
            jobDb.Delete(t.Handle, Logical.Not(Filters.LastDone.IsNull()));

            var fetched = jobDb.Query(t.Handle, QueryBuilder.All()).Single();

            CompareJobs(active, fetched);
        }
    }

    void CompareJobs(IJobDetails a, IJobDetails b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Status, b.Status);
        Assert.AreEqual(a.Errors, b.Errors);

        CompareDates(a.Created, b.Created);
        CompareDates(a.NextDue, b.NextDue);
        CompareDates(a.LastDone, b.LastDone);

        Assert.IsTrue((a.Args == null && b.Args == null)
                      || (a.Args != null && b.Args != null && a.Args.SequenceEqual(b.Args)));
    }

    void CompareDates(DateTime? a, DateTime? b)
    {
        Assert.IsTrue((a.HasValue && b.HasValue) || (!a.HasValue && !b.HasValue));

        if (a.HasValue && b.HasValue)
        {
            var diff = Convert.ToInt32((a.Value - b.Value).TotalSeconds);

            Assert.AreEqual(0, diff);
        }
    }
}