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
using LinqToDB.Configuration;
using NUnit.Framework;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.JobRunner.LinqToDB;

namespace zcfux.JobRunner.Test;

public sealed class LinqToDBTests
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    [SetUp]
    [TearDown]
    public void DeleteTestJobs()
    {
        var queue = CreateQueue();

        queue.Delete();
    }

    static JobQueue CreateQueue()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var builder = new LinqToDBConnectionOptionsBuilder();

        builder.UsePostgreSQL(connectionString);

        var opts = builder.Build();

        var engine = new Engine(opts);

        engine.Setup();

        var options = new LinqToDB.Options(TimeSpan.FromMinutes(1));

        var queue = new JobQueue(engine, options);

        return queue;
    }

    [Test]
    public void QueueIsPersistent()
    {
        // Create queue & insert job.
        var queue = CreateQueue();

        var guid = queue.Create<Jobs.Simple>().Guid;

        // Start runner with a new queue & wait for job.
        var newQueue = CreateQueue();

        var runner = new Runner(newQueue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        var source = new TaskCompletionSource<Guid>();

        runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        runner.Start();

        Assert.AreEqual(guid, source.Task.Result);

        runner.Stop();
    }

    [Test]
    public void Query_Guid()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        queue.Create<Jobs.Simple>();

        var qb = new QueryBuilder()
            .WithFilter(Filters.Guid.EqualTo(first.Guid));

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(first, fetched);
    }

    [Test]
    public void Query_Type()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        queue.Create<Jobs.Fail>();

        var qb = new QueryBuilder()
            .WithFilter(Filters.Type.EqualTo(typeof(Jobs.Simple).FullName!));

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(first, fetched);
    }

    [Test]
    public void Query_Created()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        Thread.Sleep(1000);

        var second = queue.Create<Jobs.Simple>();

        var diff = Convert.ToInt32((second.Created - first.Created).TotalSeconds);

        Assert.Greater(diff, 0);

        var qb = new QueryBuilder()
            .WithFilter(Filters.Created.EqualTo(first.Created));

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(first, fetched);
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

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(active, fetched);
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

        var qb = new QueryBuilder()
            .WithFilter(Filters.Errors.EqualTo(0));

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(active, fetched);
    }

    [Test]
    public void Query_NextDue()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();

        var second = queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        var qb = new QueryBuilder()
            .WithFilter(Filters.NextDue.GreaterThan(DateTime.UtcNow));

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(second, fetched);
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

        var qb = new QueryBuilder()
            .WithFilter(Filters.LastDone.IsNull());

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(active, fetched);
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

        var fetched = queue.Query(qb.Build()).ToArray();

        CompareJobs(jobs[0], fetched[0]);
        CompareJobs(jobs[1], fetched[1]);
        CompareJobs(jobs[2], fetched[2]);
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

        var qb = new QueryBuilder()
            .WithOrderBy("Type");

        var fetched = queue.Query(qb.Build()).ToArray();

        CompareJobs(jobs[0], fetched[0]);
        CompareJobs(jobs[1], fetched[1]);
        CompareJobs(jobs[2], fetched[2]);
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

        var qb = new QueryBuilder()
            .WithOrderBy("Created");

        var fetched = queue.Query(qb.Build()).ToArray();

        CompareJobs(jobs[0], fetched[0]);
        CompareJobs(jobs[1], fetched[1]);
        CompareJobs(jobs[2], fetched[2]);
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

        var qb = new QueryBuilder()
            .WithOrderBy("Status");

        var fetched = queue.Query(qb.Build()).ToArray();

        Assert.Less(fetched[0].Status, fetched[1].Status);

        qb = new QueryBuilder()
            .WithOrderByDescending("Status");

        fetched = queue.Query(qb.Build()).ToArray();

        Assert.Less(fetched[1].Status, fetched[0].Status);
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

        var qb = new QueryBuilder()
            .WithOrderBy("Errors");

        var fetched = queue.Query(qb.Build()).ToArray();

        Assert.Less(fetched[0].Errors, fetched[1].Errors);
    }

    [Test]
    public void OrderBy_NextDue()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        var second = queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        var qb = new QueryBuilder()
            .WithOrderByDescending("NextDue");

        var fetched = queue.Query(qb.Build()).ToArray();

        CompareJobs(first, fetched[1]);
        CompareJobs(second, fetched[0]);
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

        var qb = new QueryBuilder()
            .WithOrderByDescending(Filters.LastDone);

        var fetched = queue.Query(qb.Build()).ToArray();

        Assert.AreEqual(first, fetched[1].Guid);
        Assert.AreEqual(second, fetched[0].Guid);
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

        var qb = new QueryBuilder()
            .WithOrderBy("Guid")
            .WithSkip(2);

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(fetched, jobs.Last());
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

        var qb = new QueryBuilder()
            .WithOrderBy("Guid")
            .WithLimit(1);

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(fetched, jobs.First());
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

        var qb = new QueryBuilder()
            .WithOrderBy("Guid")
            .WithSkip(1)
            .WithLimit(1);

        var fetched = queue.Query(qb.Build()).Single();

        CompareJobs(fetched, jobs[1]);
    }

    [Test]
    public void Delete()
    {
        var queue = CreateQueue();

        queue.Create<Jobs.Simple>();
        queue.Create<Jobs.Simple>();

        queue.Delete();

        Assert.IsEmpty(queue.Query(QueryBuilder.All()));
    }

    [Test]
    public void Delete_Guid()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();
        var second = queue.Create<Jobs.Simple>();

        queue.Delete(Filters.Guid.EqualTo(first.Guid));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(second, fetched);
    }

    [Test]
    public void Delete_Type()
    {
        var queue = (CreateQueue() as JobQueue)!;

        queue.Create<Jobs.Fail>();

        var second = queue.Create<Jobs.Simple>();

        queue.Delete(Filters.Type.EqualTo(typeof(Jobs.Fail).FullName!));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(second, fetched);
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

        queue.Delete(Filters.Created.EqualTo(first.Created));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(second, fetched);
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

        var active = queue.Create<Jobs.Simple>();

        queue.Delete(Filters.Status.EqualTo(EStatus.Done));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(active, fetched);
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

        queue.Delete(Filters.Errors.GreaterThan(0));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(active, fetched);
    }

    [Test]
    public void Delete_NextDue()
    {
        var queue = CreateQueue();

        var first = queue.Create<Jobs.Simple>();

        queue.Schedule<Jobs.Simple>(DateTime.UtcNow.AddMinutes(1));

        queue.Delete(Filters.NextDue.GreaterThan(DateTime.UtcNow));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(first, fetched);
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

        queue.Delete(Logical.Not(Filters.LastDone.IsNull()));

        var fetched = queue.Query(QueryBuilder.All()).Single();

        CompareJobs(active, fetched);
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