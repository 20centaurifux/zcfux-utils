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
using zcfux.Data.Proxy;
using zcfux.Data.Test.LinqToDB;

namespace zcfux.Data.Test;

public sealed class StatefulProxyTests
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    [SetUp]
    public void Setup()
        => ResetDb();

    [TearDown]
    public void Teardown()
        => ResetDb();

    void ResetDb()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var db = Factory.PrependHandle<IStateful, Stateful>(t.Handle);

            db.DeleteAll();

            t.Commit = true;
        }
    }

    [Test]
    public void CreateProxies()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var first = Factory.PrependHandle<IStateful, Stateful>(t.Handle);

            Assert.IsInstanceOf<IStateful>(first);

            var second = Factory.PrependHandle<IStateful, Stateful>(new Stateful(), t.Handle);

            Assert.IsInstanceOf<IStateful>(second);
        }
    }

    [Test]
    public void InterceptMethods()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var db = Factory.PrependHandle<IStateful, Stateful>(t.Handle);

            var model = db.Insert(1, "hello world");

            Assert.AreEqual(1, model.ID);
            Assert.AreEqual("hello world", model.Value);

            var fetched = db.All().ToArray();

            Assert.AreEqual(1, fetched.Length);
            Assert.AreEqual(1, fetched.First().ID);
            Assert.AreEqual("hello world", fetched.First().Value);
        }
    }

    [Test]
    public void InterceptOverloadedMethod()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var db = Factory.PrependHandle<IStateful, Stateful>(t.Handle);

            var first = db.Insert(1, "hello");

            Assert.AreEqual(1, first.ID);
            Assert.AreEqual("hello", first.Value);

            var second = db.Insert("world", 2);

            Assert.AreEqual(2, second.ID);
            Assert.AreEqual("world", second.Value);
        }
    }

    [Test]
    public void MethodNotImplemented()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var db = Factory.PrependHandle<IStateful, Stateful>(t.Handle);

            Assert.That(() => db.NotImplemented(), Throws.Exception);
        }
    }

    static IEngine NewEngine()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var opts = new DataOptions()
            .UsePostgreSQL(connectionString);

        return new Engine(opts);
    }
}