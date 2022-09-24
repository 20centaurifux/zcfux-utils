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

namespace zcfux.User.Test;

public abstract class AUserDbTests
{
    IEngine _engine = null!;
    IUserDb _db = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _db = CreateUserDb();
    }

    [Test]
    public void WriteOrigin()
    {
        var origin = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Id.EqualTo(origin.Id));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).Single();

            Assert.AreEqual(origin.Id, received.Id);
            Assert.AreEqual(origin.Name, received.Name);
            Assert.AreEqual(origin.Writable, received.Writable);
        }
    }

    [Test]
    public void OverwriteOrigin()
    {
        var first = Origin.Random();

        var second = new Origin(
            first.Id,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.NextBool());

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Id.EqualTo(first.Id));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).Single();

            Assert.AreEqual(second.Id, received.Id);
            Assert.AreEqual(second.Name, received.Name);
            Assert.AreEqual(second.Writable, received.Writable);
        }
    }

    [Test]
    public void WriteOriginWithNameConflict()
    {
        var first = Origin.Random();

        var second = new Origin(
            TestContext.CurrentContext.Random.Next(),
            first.Name,
            TestContext.CurrentContext.Random.NextBool());

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);

            Assert.Throws<ConflictException>(() => _db.WriteOrigin(t.Handle, second));
        }
    }

    [Test]
    public void DeleteOrigin()
    {
        var origin = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.DeleteOrigin(t.Handle, origin.Id);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Id.EqualTo(origin.Id));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void Query_Origin_Id()
    {
        var first = Origin.Random();
        var second = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Id.EqualTo(first.Id));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).Single();

            Assert.AreEqual(first.Id, received.Id);
            Assert.AreEqual(first.Name, received.Name);
            Assert.AreEqual(first.Writable, received.Writable);
        }
    }

    [Test]
    public void OrderBy_Origin_Id()
    {
        var first = Origin.Random();
        var second = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(OriginFilters.Id);

            var received = _db.QueryOrigins(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Id, received[1].Id);
        }
    }

    [Test]
    public void Query_Origin_Name()
    {
        var first = Origin.Random();
        var second = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Name.EqualTo(first.Name));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).Single();

            Assert.AreEqual(first.Id, received.Id);
            Assert.AreEqual(first.Name, received.Name);
            Assert.AreEqual(first.Writable, received.Writable);
        }
    }

    [Test]
    public void OrderBy_Origin_Name()
    {
        var first = Origin.Random();
        var second = Origin.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(OriginFilters.Name);

            var received = _db.QueryOrigins(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Name, received[1].Name);
        }
    }

    [Test]
    public void Query_Origin_Writable()
    {
        var first = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            false);

        var second = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            true);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(OriginFilters.Writable.EqualTo(first.Writable));

            var received = _db.QueryOrigins(t.Handle, qb.Build()).Single();

            Assert.AreEqual(first.Id, received.Id);
            Assert.AreEqual(first.Name, received.Name);
            Assert.AreEqual(first.Writable, received.Writable);
        }
    }

    [Test]
    public void OrderBy_Origin_Writable()
    {
        var first = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            false);

        var second = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            true);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(OriginFilters.Writable);

            var received = _db.QueryOrigins(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(first.Writable, second.Writable);
        }
    }

    [Test]
    public void Origin_Skip()
    {
        var first = new Origin(
            TestContext.CurrentContext.Random.Next(),
            "a",
            TestContext.CurrentContext.Random.NextBool());

        var second = new Origin(
            TestContext.CurrentContext.Random.Next(),
            "b",
            TestContext.CurrentContext.Random.NextBool());

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(OriginFilters.Name)
                .WithSkip(1);

            var received = _db
                .QueryOrigins(t.Handle, qb.Build())
                .Single();

            Assert.AreEqual(second.Id, received.Id);
            Assert.AreEqual(second.Name, received.Name);
            Assert.AreEqual(second.Writable, received.Writable);
        }
    }

    [Test]
    public void Origin_Limit()
    {
        var first = new Origin(
            TestContext.CurrentContext.Random.Next(),
            "a",
            TestContext.CurrentContext.Random.NextBool());

        var second = new Origin(
            TestContext.CurrentContext.Random.Next(),
            "b",
            TestContext.CurrentContext.Random.NextBool());

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, first);
            _db.WriteOrigin(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(OriginFilters.Name)
                .WithLimit(1);

            var received = _db
                .QueryOrigins(t.Handle, qb.Build())
                .Single();

            Assert.AreEqual(first.Id, received.Id);
            Assert.AreEqual(first.Name, received.Name);
            Assert.AreEqual(first.Writable, received.Writable);
        }
    }

    [Test]
    public void WriteUser()
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, user);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(user.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(user, received);
        }
    }

    [Test]
    public void OverwriteUser()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();

        var second = new User(
            first.Guid,
            firstOrigin,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Locked);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(first.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(second, received);
        }
    }

    [Test]
    public void WriteUserWithSameNameAndDifferentOrigin()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();

        var second = new User(
            Guid.NewGuid(),
            secondOrigin,
            first.Name,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(first.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);

            qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(second.Guid));

            received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(second, received);
        }
    }

    [Test]
    public void WriteUserWithNameAndOriginConflict()
    {
        var origin = Origin.Random();

        var first = User.Random(origin);

        var second = new User(
            Guid.NewGuid(),
            origin,
            first.Name,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);

            Assert.Throws<ConflictException>(() => _db.WriteUser(t.Handle, second));
        }
    }

    [Test]
    public void WriteUserWithNonExistingOrigin()
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            Assert.That(() => _db.WriteUser(t.Handle, user), Throws.Exception);
        }
    }

    [Test]
    public void DeleteUser()
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, user);
            _db.DeleteUser(t.Handle, user.Guid);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(user.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteOriginWithAssociatedUser()
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, user);
            _db.DeleteOrigin(t.Handle, origin.Id);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(user.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void Query_User_Guid()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Guid.EqualTo(first.Guid));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Guid()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Guid);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Guid.CompareTo(received[1].Guid), 0);
        }
    }

    [Test]
    public void Query_User_Name()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Name.EqualTo(first.Name));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Name()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Name);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Name, received[1].Name);
        }
    }

    [Test]
    public void Query_User_OriginId()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();
        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.OriginId.EqualTo(first.Origin.Id));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_OriginId()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();
        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.OriginId);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Origin.Id, received[1].Origin.Id);
        }
    }

    [Test]
    public void Query_User_Origin()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();
        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Origin.EqualTo(first.Origin.Name));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Origin()
    {
        var firstOrigin = Origin.Random();
        var first = User.Random(firstOrigin);

        var secondOrigin = Origin.Random();
        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Origin);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Origin.Name, received[1].Origin.Name);
        }
    }

    [Test]
    public void Query_User_Writable()
    {
        var firstOrigin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            true);

        var first = User.Random(firstOrigin);

        var secondOrigin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            false);

        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Writable.EqualTo(first.Origin.Writable));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Writable()
    {
        var firstOrigin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            true);

        var first = User.Random(firstOrigin);

        var secondOrigin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            false);

        var second = User.Random(secondOrigin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, firstOrigin);
            _db.WriteUser(t.Handle, first);

            _db.WriteOrigin(t.Handle, secondOrigin);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Writable);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Origin.Writable, received[1].Origin.Writable);
        }
    }

    public void Query_User_Status()
    {
        var origin = Origin.Random();

        var first = new User(
            Guid.NewGuid(),
            origin,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        var second = new User(
            Guid.NewGuid(),
            origin,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Locked);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Status.EqualTo(first.Status));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    public void OrderBy_User_Status()
    {
        var origin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.NextBool());

        var first = new User(
            Guid.NewGuid(),
            origin,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        var second = new User(
            Guid.NewGuid(),
            origin,
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Locked);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Status);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Status, received[1].Status);
        }
    }

    [Test]
    public void Query_User_Firstname()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Firstname.EqualTo(first.Firstname!));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Firstname()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Firstname);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Firstname!, received[1].Firstname);
        }
    }

    [Test]
    public void Query_User_Lastname()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(UserFilters.Lastname.EqualTo(first.Lastname!));

            var received = _db.QueryUsers(t.Handle, qb.Build()).Single();

            AssertUser(first, received);
        }
    }

    [Test]
    public void OrderBy_User_Lastname()
    {
        var origin = Origin.Random();
        var first = User.Random(origin);
        var second = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Lastname);

            var received = _db.QueryUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Lastname!, received[1].Lastname);
        }
    }


    [Test]
    public void User_Skip()
    {
        var origin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.NextBool());

        var first = new User(
            Guid.NewGuid(),
            origin,
            "a",
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        var second = new User(
            Guid.NewGuid(),
            origin,
            "b",
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Name)
                .WithSkip(1);

            var received = _db
                .QueryUsers(t.Handle, qb.Build())
                .Single();

            AssertUser(second, received);
        }
    }

    [Test]
    public void User_Limit()
    {
        var origin = new Origin(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.NextBool());

        var first = new User(
            Guid.NewGuid(),
            origin,
            "a",
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        var second = new User(
            Guid.NewGuid(),
            origin,
            "b",
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString(),
            EStatus.Active);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteOrigin(t.Handle, origin);
            _db.WriteUser(t.Handle, first);
            _db.WriteUser(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(UserFilters.Name)
                .WithLimit(1);

            var received = _db
                .QueryUsers(t.Handle, qb.Build())
                .Single();

            AssertUser(first, received);
        }
    }

    static void AssertUser(IUser a, IUser b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Origin.Id, b.Origin.Id);
        Assert.AreEqual(a.Origin.Name, b.Origin.Name);
        Assert.AreEqual(a.Name, b.Name);
        Assert.AreEqual(a.Firstname, b.Firstname);
        Assert.AreEqual(a.Lastname, b.Lastname);
        Assert.AreEqual(a.Origin.Writable, b.Origin.Writable);
        Assert.AreEqual(a.Status, b.Status);
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract IUserDb CreateUserDb();
}