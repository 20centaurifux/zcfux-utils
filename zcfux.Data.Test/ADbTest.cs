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
using zcfux.Data.Proxy;

namespace zcfux.Data.Test;

public abstract class ADbTest
{
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
            var db = NewDb();

            db.DeleteAll(t.Handle);

            t.Commit = true;
        }
    }

    [Test]
    public void Commit()
    {
        var engine = NewEngine();

        engine.Setup();

        Model created;

        var db = NewDb();

        using (var t = engine.NewTransaction())
        {
            created = db.New(t.Handle, TestContext.CurrentContext.Random.GetString());

            t.Commit = true;
        }

        Model[] fetched;

        using (var t = engine.NewTransaction())
        {
            fetched = db.All(t.Handle).ToArray();
        }

        Assert.AreEqual(1, fetched.Length);

        Assert.AreEqual(created.ID, fetched.First().ID);
        Assert.AreEqual(created.Value, fetched.First().Value);
    }

    [Test]
    public void Rollback()
    {
        var engine = NewEngine();

        engine.Setup();

        var db = NewDb();

        using (var t = engine.NewTransaction())
        {
            db.New(t.Handle, TestContext.CurrentContext.Random.GetString());
        }

        Model[] fetched;

        using (var t = engine.NewTransaction())
        {
            fetched = db.All(t.Handle).ToArray();
        }

        Assert.AreEqual(0, fetched.Length);
    }

    [Test]
    public void StatefulDb()
    {
        var engine = NewEngine();

        engine.Setup();

        using (var t = engine.NewTransaction())
        {
            var db = Factory.CreateStatefulProxy<IStatefulTestAccess>(t.Handle, NewDb());

            var created = db.New(TestContext.CurrentContext.Random.GetString());

            var fetched = db.All().ToArray();

            Assert.AreEqual(1, fetched.Length);

            Assert.AreEqual(created.ID, fetched.First().ID);
            Assert.AreEqual(created.Value, fetched.First().Value);
        }
    }

    protected abstract IEngine NewEngine();

    protected abstract ITestDb NewDb();
}