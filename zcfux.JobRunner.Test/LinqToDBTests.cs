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

namespace zcfux.JobRunner.Test;

public sealed class LinqtoDBTests : ARunnerTests
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    protected override AJobQueue CreateQueue()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var builder = new LinqToDbConnectionOptionsBuilder();

        builder.UsePostgreSQL(connectionString);

        var opts = builder.Build();

        var engine = new Engine(opts);

        engine.Setup();

        var queue = new LinqToDB.JobQueue(engine);

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
}