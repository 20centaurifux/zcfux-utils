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
using System.Globalization;
using NUnit.Framework;
using zcfux.Application;
using zcfux.Data;
using zcfux.Filter;

namespace zcfux.User.Test;

public abstract class AGroupDbTests
{
    static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");
        
    IEngine _engine = null!;
    IGroupDb _groupDb = null!;
    IUserDb _userDb = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _groupDb = CreateGroupDb();
        _userDb = CreateUserDb();
    }

    [Test]
    public void WriteGroup()
    {
        var group = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, group);

            var qb = new QueryBuilder()
                .WithFilter(GroupFilters.Guid.EqualTo(group.Guid));

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).Single();

            AssertGroup(group, received);
        }
    }

    [Test]
    public void OverwriteGroup()
    {
        var first = Group.Random();

        var second = new Group(
            first.Guid,
            TestContext.CurrentContext.Random.GetString());

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);
            _groupDb.WriteGroup(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(GroupFilters.Guid.EqualTo(first.Guid));

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).Single();

            AssertGroup(second, received);
        }
    }

    [Test]
    public void WriteGroupWithNameConflict()
    {
        var first = Group.Random();

        var second = new Group(
            Guid.NewGuid(),
            first.Name);

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);

            Assert.Throws<ConflictException>(() => _groupDb.WriteGroup(t.Handle, second));
        }
    }

    [Test]
    public void DeleteGroup()
    {
        var group = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, group);
            _groupDb.DeleteGroup(t.Handle, group.Guid);

            var qb = new QueryBuilder()
                .WithFilter(GroupFilters.Guid.EqualTo(group.Guid));

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void Query_Group_Guid()
    {
        var first = Group.Random();
        var second = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);
            _groupDb.WriteGroup(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(GroupFilters.Guid.EqualTo(first.Guid));

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).Single();

            AssertGroup(first, received);
        }
    }

    [Test]
    public void OrderBy_Group_Guid()
    {
        var first = Group.Random();
        var second = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);
            _groupDb.WriteGroup(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(GroupFilters.Guid);

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Guid.CompareTo(received[1].Guid), 0);
        }
    }

    [Test]
    public void Query_Group_Name()
    {
        var first = Group.Random();
        var second = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);
            _groupDb.WriteGroup(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(GroupFilters.Name.EqualTo(first.Name));

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).Single();

            AssertGroup(first, received);
        }
    }

    [Test]
    public void OrderBy_Group_Name()
    {
        var first = Group.Random();
        var second = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            _groupDb.WriteGroup(t.Handle, first);
            _groupDb.WriteGroup(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(GroupFilters.Name);

            var received = _groupDb.QueryGroups(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].Name,
                received[1].Name,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void AssignUser()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.UserUid.EqualTo(user.Guid));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(user, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void AssignUserTwice()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, user);

            Assert.Throws<ConflictException>(() => _groupDb.AssignUser(t.Handle, site, group, user));
        }
    }

    [Test]
    public void AssignNonExistingSite()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);

            var site = new Site(Guid.NewGuid(), TestContext.CurrentContext.Random.GetString());

            Assert.Throws<NotFoundException>(() => _groupDb.AssignUser(t.Handle, site, group, user));
        }
    }

    [Test]
    public void AssignNonExistingGroup()
    {
        var group = Group.Random();

        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var site = CreateSite(t.Handle);

            Assert.Throws<NotFoundException>(() => _groupDb.AssignUser(t.Handle, site, group, user));
        }
    }

    [Test]
    public void AssignNonExistingUser()
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        using (var t = _engine.NewTransaction())
        {
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            Assert.Throws<NotFoundException>(() => _groupDb.AssignUser(t.Handle, site, group, user));
        }
    }

    [Test]
    public void UnassignUser()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, user);
            _groupDb.UnassignUser(t.Handle, site, group, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.UserUid.EqualTo(user.Guid));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.IsEmpty(received);
        }
    }

    [Test]
    public void UnassignNonExistingEntry()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, user);
            _groupDb.UnassignUser(t.Handle, site, group, user);

            Assert.Throws<NotFoundException>(() => _groupDb.UnassignUser(t.Handle, site, group, user));
        }
    }

    [Test]
    public void Query_AssignedUser_SiteUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);

            var firstSite = CreateSite(t.Handle);
            var secondSite = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, firstSite, group, user);
            _groupDb.AssignUser(t.Handle, secondSite, group, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.SiteUid.EqualTo(firstSite.Guid));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(user, received.User);
            AssertSite(firstSite, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_SiteUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);

            var firstSite = CreateSite(t.Handle);
            var secondSite = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, firstSite, group, user);
            _groupDb.AssignUser(t.Handle, secondSite, group, user);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.SiteUid);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Site.Guid.CompareTo(received[1].Site.Guid), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Site()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);

            var firstSite = CreateSite(t.Handle);
            var secondSite = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, firstSite, group, user);
            _groupDb.AssignUser(t.Handle, secondSite, group, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Site.EqualTo(firstSite.Name));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(user, received.User);
            AssertSite(firstSite, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Site()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);

            var firstSite = CreateSite(t.Handle);
            var secondSite = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, firstSite, group, user);
            _groupDb.AssignUser(t.Handle, secondSite, group, user);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Site);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].Site.Name,
                received[1].Site.Name,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_GroupUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, firstGroup, user);
            _groupDb.AssignUser(t.Handle, site, secondGroup, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.GroupUid.EqualTo(firstGroup.Guid));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(firstGroup, received.Group);
            AssertUser(user, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_GroupUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, firstGroup, user);
            _groupDb.AssignUser(t.Handle, site, secondGroup, user);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.GroupUid);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Group.Guid.CompareTo(received[1].Group.Guid), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Group()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, firstGroup, user);
            _groupDb.AssignUser(t.Handle, site, secondGroup, user);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Group.EqualTo(firstGroup.Name));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(firstGroup, received.Group);
            AssertUser(user, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Group()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);
            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, firstGroup, user);
            _groupDb.AssignUser(t.Handle, site, secondGroup, user);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Group);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].Group.Name,
                received[1].Group.Name,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_OriginId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.OriginId.EqualTo(firstUser.Origin.Id));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_OriginId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.OriginId);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].User.Origin.Id.CompareTo(received[1].User.Origin.Id), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Origin()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Origin.EqualTo(firstUser.Origin.Name));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Origin()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Origin);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].User.Origin.Name,
                received[1].User.Origin.Name,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Writable()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstOrigin = new Origin(
                TestContext.CurrentContext.Random.Next(),
                TestContext.CurrentContext.Random.GetString(),
                true);

            var firstUser = User.Random(firstOrigin);

            _userDb.WriteOrigin(t.Handle, firstOrigin);
            _userDb.WriteUser(t.Handle, firstUser);

            var secondOrigin = new Origin(
                TestContext.CurrentContext.Random.Next(),
                TestContext.CurrentContext.Random.GetString(),
                false);

            var secondUser = User.Random(secondOrigin);

            _userDb.WriteOrigin(t.Handle, secondOrigin);
            _userDb.WriteUser(t.Handle, secondUser);

            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Writable.EqualTo(firstUser.Origin.Writable));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Writable()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstOrigin = new Origin(
                TestContext.CurrentContext.Random.Next(),
                TestContext.CurrentContext.Random.GetString(),
                true);

            var firstUser = User.Random(firstOrigin);

            _userDb.WriteOrigin(t.Handle, firstOrigin);
            _userDb.WriteUser(t.Handle, firstUser);

            var secondOrigin = new Origin(
                TestContext.CurrentContext.Random.Next(),
                TestContext.CurrentContext.Random.GetString(),
                false);

            var secondUser = User.Random(secondOrigin);

            _userDb.WriteOrigin(t.Handle, secondOrigin);
            _userDb.WriteUser(t.Handle, secondUser);

            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Writable);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].User.Origin.Writable.CompareTo(received[1].User.Origin.Writable), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_UserUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.UserUid.EqualTo(firstUser.Guid));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_UserUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.UserUid);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].User.Guid.CompareTo(received[1].User.Guid), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Username()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Username.EqualTo(firstUser.Name));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Username()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Username);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].User.Name,
                received[1].User.Name,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Firstname()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Firstname.EqualTo(firstUser.Firstname!));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Firstname()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Firstname);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].User.Firstname!,
                received[1].User.Firstname,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Lastname()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Lastname.EqualTo(firstUser.Lastname!));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Lastname()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstUser = CreateUser(t.Handle);
            var secondUser = CreateUser(t.Handle);
            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Lastname);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(string.Compare(
                received[0].User.Lastname!,
                received[1].User.Lastname,
                Culture,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase), 0);
        }
    }

    [Test]
    public void Query_AssignedUser_Status()
    {
        using (var t = _engine.NewTransaction())
        {
            var origin = Origin.Random();

            var firstUser = new User(
                Guid.NewGuid(),
                origin,
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                EStatus.Active
            );

            var secondUser = new User(
                Guid.NewGuid(),
                origin,
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                EStatus.Locked
            );

            _userDb.WriteOrigin(t.Handle, origin);
            _userDb.WriteUser(t.Handle, firstUser);
            _userDb.WriteUser(t.Handle, secondUser);

            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithFilter(AssignedUserFilters.Status.EqualTo(firstUser.Status));

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).Single();

            AssertGroup(group, received.Group);
            AssertUser(firstUser, received.User);
            AssertSite(site, received.Site);
        }
    }

    [Test]
    public void OrderBy_AssignedUser_Status()
    {
        using (var t = _engine.NewTransaction())
        {
            var origin = Origin.Random();

            var firstUser = new User(
                Guid.NewGuid(),
                origin,
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                EStatus.Active
            );

            var secondUser = new User(
                Guid.NewGuid(),
                origin,
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString(),
                EStatus.Locked
            );

            _userDb.WriteOrigin(t.Handle, origin);
            _userDb.WriteUser(t.Handle, firstUser);
            _userDb.WriteUser(t.Handle, secondUser);

            var group = CreateGroup(t.Handle);
            var site = CreateSite(t.Handle);

            _groupDb.AssignUser(t.Handle, site, group, firstUser);
            _groupDb.AssignUser(t.Handle, site, group, secondUser);

            var qb = new QueryBuilder()
                .WithOrderBy(AssignedUserFilters.Status);

            var received = _groupDb.QueryAssignedUsers(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].User.Status.CompareTo(received[1].User.Status), 0);
        }
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract IGroupDb CreateGroupDb();

    protected abstract IUserDb CreateUserDb();

    protected abstract ISiteDb CreateSiteDb();

    IUser CreateUser(object handle)
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        _userDb.WriteOrigin(handle, origin);
        _userDb.WriteUser(handle, user);

        return user;
    }

    IGroup CreateGroup(object handle)
    {
        var group = Group.Random();

        _groupDb.WriteGroup(handle, group);

        return group;
    }

    ISite CreateSite(object handle)
    {
        var site = new Site(
            Guid.NewGuid(),
            TestContext.CurrentContext.Random.GetString());

        var db = CreateSiteDb();

        db.Write(handle, site);

        return site;
    }

    static void AssertGroup(IGroup a, IGroup b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Name, b.Name);
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

    static void AssertSite(ISite a, ISite b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Name, b.Name);
    }
}