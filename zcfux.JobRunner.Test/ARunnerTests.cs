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

namespace zcfux.JobRunner.Test;

public abstract class ARunnerTests
{
    AJobQueue _queue = default!;
    Runner _runner = default!;

    [SetUp]
    public void Setup()
    {
        _queue = CreateQueue();

        _queue.Setup();

        _runner = new Runner(_queue, new(MaxJobs: 2, MaxErrors: 2, RetrySecs: 1));

        _runner.Start();
    }

    protected abstract AJobQueue CreateQueue();

    [TearDown]
    public void TearDown()
        => _runner.Stop();

    [Test]
    public void RunSimpleJob()
    {
        var source = new TaskCompletionSource<Guid>();

        _runner.Done += (s, e) => source.TrySetResult(e.Job.Guid);

        var job = _queue.Create<Jobs.Simple>();

        Assert.AreEqual(job.Guid, source.Task.Result);
    }

    [Test]
    public void RunFailingJob()
    {
        var failedSource = new TaskCompletionSource<Guid>();

        _runner.Failed += (s, e) => failedSource.TrySetResult(e.Job.Guid);

        var doneSource = new TaskCompletionSource<Guid>();

        _runner.Done += (s, e) => doneSource.TrySetResult(e.Job.Guid);

        Jobs.Fail.Fails = 1;

        var job = _queue.Create<Jobs.Fail>();

        Assert.AreEqual(job.Guid, failedSource.Task.Result);
        Assert.AreEqual(job.Guid, doneSource.Task.Result);
    }

    [Test]
    public void RunAbortingJob()
    {
        var abortSource = new TaskCompletionSource<Guid>();

        _runner.Aborted += (s, e) => abortSource.TrySetResult(e.Job.Guid);

        var doneSource = new TaskCompletionSource<Guid>();

        _runner.Done += (s, e) => doneSource.SetResult(e.Job.Guid);

        Jobs.Fail.Fails = 3;

        var job = _queue.Create<Jobs.Fail>();

        Assert.AreEqual(job.Guid, abortSource.Task.Result);

        var completed = doneSource.Task.Wait(3000);

        Assert.IsFalse(completed);
    }

    [Test]
    public void RunJobWithCustomNextDueDate()
    {
        long counter = 0;

        _runner.Done += (s, e) => Interlocked.Increment(ref counter);

        _queue.Create<Jobs.Twice>();

        Thread.Sleep(1000);

        var count = Interlocked.Read(ref counter);

        Assert.GreaterOrEqual(2, count);
    }

    [Test]
    public void RunParallel()
    {
        long counter = 0;

        var source = new TaskCompletionSource();

        _runner.Done += (s, e) =>
        {
            Interlocked.Increment(ref counter);

            if (Interlocked.Read(ref counter) == 2)
            {
                source.SetResult();
            }
        };

        _queue.Create<Jobs.Simple>();
        _queue.Create<Jobs.Simple>();

        source.Task.Wait();

        Assert.AreEqual(2, Interlocked.Read(ref counter));
    }
}