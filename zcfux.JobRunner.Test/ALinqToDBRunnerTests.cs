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
using LinqToDB;
using NUnit.Framework;
using zcfux.Data.LinqToDB;
using zcfux.JobRunner.Data.LinqToDB;

namespace zcfux.JobRunner.Test;

public abstract class ALinqToDBRunnerTests : ARunnerTests
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    Engine? _engine;

    [TearDown]
    public void Teardown()
        => DeleteJobs();

    protected override AJobQueue CreateQueue()
    {
        if (_engine == null)
        {
            CreateAndSetupEngine();
            DeleteJobs();
        }

        var options = CreateOptions();

        var queue = new JobQueue(_engine!, options);

        return queue;
    }

    void CreateAndSetupEngine()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var opts = new DataOptions()
            .UsePostgreSQL(connectionString);

        _engine = new Engine(opts);

        _engine.Setup();
    }

    protected abstract Data.LinqToDB.Options CreateOptions();

    void DeleteJobs()
    {
        using (var t = _engine!.NewTransaction())
        {
            var queue = CreateQueue();

            (queue as JobQueue)!.Delete(t.Handle);

            t.Commit = true;
        }
    }
}