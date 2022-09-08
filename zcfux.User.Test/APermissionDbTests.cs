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
using zcfux.Application;
using zcfux.Data;
using zcfux.Filter;

namespace zcfux.User.Test;

public abstract class APermissionDbTests
{
    IEngine _engine = null!;
    IPermissionDb _permissionDb = null!;
    IGroupDb _groupDb = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _permissionDb = CreatePermissionDb();
        _groupDb = CreateGroupDb();
    }

    [Test]
    public void WritePermissionCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(category.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(category, received);
        }
    }

    [Test]
    public void OverwritePermissionCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            var secondApplication = CreateApplication(t.Handle);

            var second = new PermissionCategory(
                first.Id,
                TestContext.CurrentContext.Random.GetString(),
                secondApplication);

            _permissionDb.WritePermissionCategory(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(first.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(second, received);
        }
    }

    [Test]
    public void WritePermissionCategoryWithSameName()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            var secondApplication = CreateApplication(t.Handle);

            var second = new PermissionCategory(
                TestContext.CurrentContext.Random.Next(),
                first.Name,
                secondApplication);

            _permissionDb.WritePermissionCategory(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(first.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(first, received);

            qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(second.Id));

            received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(second, received);
        }
    }

    [Test]
    public void WritePermissionCategoryWithNameAndApplicationConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            var second = new PermissionCategory(
                TestContext.CurrentContext.Random.Next(),
                first.Name,
                first.Application);

            Assert.Throws<ConflictException>(() => _permissionDb.WritePermissionCategory(t.Handle, second));
        }
    }

    [Test]
    public void DeletePermissionCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);

            _permissionDb.DeletePermissionCategory(t.Handle, category.Id);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(category.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteNonExistingPermissionCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);

            _permissionDb.DeletePermissionCategory(t.Handle, category.Id);

            Assert.Throws<NotFoundException>(() => _permissionDb.DeletePermissionCategory(t.Handle, category.Id));
        }
    }

    [Test]
    public void Query_PermissionCategory_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Id.EqualTo(first.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(first, received);
        }
    }

    [Test]
    public void OrderBy_PermissionCategory_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            CreatePermissionCategory(t.Handle);
            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionCategoryFilters.Id);

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Id.CompareTo(received[1].Id), 0);
        }
    }

    [Test]
    public void Query_PermissionCategory_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Name.EqualTo(first.Name));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(first, received);
        }
    }

    [Test]
    public void OrderBy_PermissionCategory_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            CreatePermissionCategory(t.Handle);
            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionCategoryFilters.Name);

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Name.CompareTo(received[1].Name), 0);
        }
    }

    [Test]
    public void Query_PermissionCategory_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.ApplicationId.EqualTo(first.Application.Id));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(first, received);
        }
    }

    [Test]
    public void OrderBy_PermissionCategory_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            CreatePermissionCategory(t.Handle);
            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionCategoryFilters.ApplicationId);

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Application.Id.CompareTo(received[1].Application.Id), 0);
        }
    }

    [Test]
    public void Query_PermissionCategory_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermissionCategory(t.Handle);

            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionCategoryFilters.Application.EqualTo(first.Application.Name));

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).Single();

            AssertPermissionCategory(first, received);
        }
    }

    [Test]
    public void OrderBy_PermissionCategory_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            CreatePermissionCategory(t.Handle);
            CreatePermissionCategory(t.Handle);

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionCategoryFilters.Application);

            var received = _permissionDb.QueryPermissionCategories(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Application.Name.CompareTo(received[1].Application.Name), 0);
        }
    }

    [Test]
    public void WritePermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(permission.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OverwritePermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);

            var category = CreatePermissionCategory(t.Handle);

            var preconditions = CreatePreconditions(
                t.Handle,
                category,
                level: 0,
                maxLevel: TestContext.CurrentContext.Random.Next(0, 4));

            var second = new Permission(
                first.Id,
                TestContext.CurrentContext.Random.GetString(),
                category,
                preconditions.ToArray());

            _permissionDb.WritePermission(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(second.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(second, received);
        }
    }

    [Test]
    public void WritePermissionWithSameName()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);

            var category = CreatePermissionCategory(t.Handle);

            var preconditions = CreatePreconditions(
                t.Handle,
                category,
                level: 0,
                maxLevel: TestContext.CurrentContext.Random.Next(0, 4));

            var second = new Permission(
                TestContext.CurrentContext.Random.Next(),
                first.Name,
                category,
                preconditions.ToArray());

            _permissionDb.WritePermission(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(first.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(first, received);

            qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(second.Id));

            received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(second, received);
        }
    }

    [Test]
    public void WritePermissionWithNameAndCategoryConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);

            var preconditions = CreatePreconditions(
                t.Handle,
                first.Category,
                level: 0,
                maxLevel: TestContext.CurrentContext.Random.Next(0, 4));

            var second = new Permission(
                TestContext.CurrentContext.Random.Next(),
                first.Name,
                first.Category,
                preconditions.ToArray());

            Assert.Throws<ConflictException>(() => _permissionDb.WritePermission(t.Handle, second));
        }
    }

    [Test]
    public void DeletePermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            _permissionDb.DeletePermission(t.Handle, permission.Id);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(permission.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteNonExistingPermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            _permissionDb.DeletePermission(t.Handle, permission.Id);

            Assert.Throws<NotFoundException>(() => _permissionDb.DeletePermission(t.Handle, permission.Id));
        }
    }

    [Test]
    public void DeletePermissionCategoryWithAssociatedPermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            _permissionDb.DeletePermissionCategory(t.Handle, permission.Category.Id);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(permission.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void Query_Permission_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.EqualTo(permission.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);
            var second = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.In(first.Id, second.Id))
                .WithOrderBy(PermissionFilters.Id);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Id.CompareTo(received[1].Id), 0);
        }
    }

    [Test]
    public void Query_Permission_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Name.EqualTo(permission.Name));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);
            var second = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.In(first.Id, second.Id))
                .WithOrderBy(PermissionFilters.Name);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Name.CompareTo(received[1].Name), 0);
        }
    }

    [Test]
    public void Query_Permission_CategoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.CategoryId.EqualTo(permission.Category.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_CategoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);
            var second = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.In(first.Id, second.Id))
                .WithOrderBy(PermissionFilters.CategoryId);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Category.Id.CompareTo(received[1].Category.Id), 0);
        }
    }

    [Test]
    public void Query_Permission_Category()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Category.EqualTo(permission.Category.Name));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_Category()
    {
        using (var t = _engine.NewTransaction())
        {
            var first = CreatePermission(t.Handle);
            var second = CreatePermission(t.Handle);

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Id.In(first.Id, second.Id))
                .WithOrderBy(PermissionFilters.Category);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Category.Name.CompareTo(received[1].Category.Name), 0);
        }
    }

    [Test]
    public void Query_Permission_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.ApplicationId.EqualTo(permission.Category.Application.Id));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var first = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var second = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionFilters.ApplicationId);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Category.Application.Id.CompareTo(received[1].Category.Application.Id), 0);
        }
    }

    [Test]
    public void Query_Permission_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithFilter(PermissionFilters.Application.EqualTo(permission.Category.Application.Name));

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).Single();

            AssertPermission(permission, received);
        }
    }

    [Test]
    public void OrderBy_Permission_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var first = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var second = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var qb = new QueryBuilder()
                .WithOrderBy(PermissionFilters.Application);

            var received = _permissionDb.QueryPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Category.Application.Name.CompareTo(received[1].Category.Application.Name), 0);
        }
    }

    [Test]
    public void GrantPermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Id.EqualTo(permission.Id));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void GrantPermissionTwice()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            Assert.Throws<ConflictException>(() => _permissionDb.Grant(t.Handle, permission, group));
        }
    }

    [Test]
    public void GrantPermissionWithUnfulfilledRequirement()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);

            var parent = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var child = CreatePermission(t.Handle, category, new IPermission[] { parent });
            var group = CreateGroup(t.Handle);

            Assert.Throws<InvalidOperationException>(() => _permissionDb.Grant(t.Handle, child, group));
        }
    }

    [Test]
    public void GrantPermissionWithFulfilledRequirement()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);

            var parent = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var child = CreatePermission(t.Handle, category, new IPermission[] { parent });
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, parent, group);
            _permissionDb.Grant(t.Handle, child, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Id.In(parent.Id, child.Id));

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            var receivedParent = grantedPermissions.Where(p => p.Permission.Id == parent.Id).Single();

            AssertGroup(group, receivedParent.Group);
            AssertPermission(parent, receivedParent.Permission);

            var receivedChild = grantedPermissions.Where(p => p.Permission.Id == child.Id).Single();

            AssertGroup(group, receivedChild.Group);
            AssertPermission(child, receivedChild.Permission);
        }
    }

    [Test]
    public void RevokePermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);
            _permissionDb.Revoke(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Id.EqualTo(permission.Id));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(grantedPermission);
        }
    }

    [Test]
    public void RevokePermissionWithGrantedChildPermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var parent = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var child = CreatePermission(t.Handle, category, new IPermission[] { parent });
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, parent, group);
            _permissionDb.Grant(t.Handle, child, group);

            Assert.Throws<InvalidOperationException>(() => _permissionDb.Revoke(t.Handle, parent, group));
        }
    }

    [Test]
    public void RevokeNonExistingPermission()
    {
        using (var t = _engine.NewTransaction())
        {
            var permission = CreatePermission(t.Handle);
            var group = CreateGroup(t.Handle);

            Assert.Throws<NotFoundException>(() => _permissionDb.Revoke(t.Handle, permission, group));
        }
    }

    [Test]
    public void Query_GrantedPermission_CategoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.CategoryId.EqualTo(category.Id));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_CategoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.CategoryId);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Category.Id.CompareTo
                    (grantedPermissions[1].Permission.Category.Id),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_Category()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Category.EqualTo(category.Name));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_Category()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.Category);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Category.Name.CompareTo(
                    grantedPermissions[1].Permission.Category.Name),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_GroupUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.GroupUid.EqualTo(group.Guid));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_GroupUid()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, firstGroup);
            _permissionDb.Grant(t.Handle, permission, secondGroup);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.GroupUid);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Group.Guid.CompareTo(
                    grantedPermissions[1].Group.Guid),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_Group()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Group.EqualTo(group.Name));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_Group()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());

            var firstGroup = CreateGroup(t.Handle);
            var secondGroup = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, firstGroup);
            _permissionDb.Grant(t.Handle, permission, secondGroup);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.Group);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Group.Name.CompareTo(
                    grantedPermissions[1].Group.Name),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Id.EqualTo(permission.Id));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var secondPermission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.Id);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Id.CompareTo(
                    grantedPermissions[1].Permission.Id),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var permission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, permission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Name.EqualTo(permission.Name));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(permission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_Name()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var secondPermission = CreatePermission(t.Handle, category, Enumerable.Empty<IPermission>());
            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.Name);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Name.CompareTo(
                    grantedPermissions[1].Permission.Name),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.ApplicationId.EqualTo(firstCategory.Application.Id));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(firstPermission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_ApplicationId()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.ApplicationId);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Category.Application.Id.CompareTo
                    (grantedPermissions[1].Permission.Category.Application.Id),
                0);
        }
    }

    [Test]
    public void Query_GrantedPermission_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithFilter(GrantedPermissionFilters.Application.EqualTo(firstCategory.Application.Name));

            var grantedPermission = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).Single();

            AssertGroup(group, grantedPermission.Group);
            AssertPermission(firstPermission, grantedPermission.Permission);
        }
    }

    [Test]
    public void OrderBy_GrantedPermission_Application()
    {
        using (var t = _engine.NewTransaction())
        {
            var firstCategory = CreatePermissionCategory(t.Handle);
            var firstPermission = CreatePermission(t.Handle, firstCategory, Enumerable.Empty<IPermission>());

            var secondCategory = CreatePermissionCategory(t.Handle);
            var secondPermission = CreatePermission(t.Handle, secondCategory, Enumerable.Empty<IPermission>());

            var group = CreateGroup(t.Handle);

            _permissionDb.Grant(t.Handle, firstPermission, group);
            _permissionDb.Grant(t.Handle, secondPermission, group);

            var qb = new QueryBuilder()
                .WithOrderBy(GrantedPermissionFilters.Application);

            var grantedPermissions = _permissionDb.QueryGrantedPermissions(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, grantedPermissions.Length);

            Assert.Less(
                grantedPermissions[0].Permission.Category.Application.Name.CompareTo
                    (grantedPermissions[1].Permission.Category.Application.Name),
                0);
        }
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract IPermissionDb CreatePermissionDb();

    protected abstract IGroupDb CreateGroupDb();

    protected abstract IApplicationDb CreateApplicationDb();

    protected abstract ISiteDb CreateSiteDb();

    IPermission CreatePermission(object handle)
    {
        var category = CreatePermissionCategory(handle);
        var permission = CreatePermission(handle, category);

        return permission;
    }

    IPermission CreatePermission(object handle, IPermissionCategory category)
    {
        var preconditions = CreatePreconditions(
            handle,
            category,
            level: 0,
            maxLevel: TestContext.CurrentContext.Random.Next(0, 4));

        var permission = Permission.Random(category, preconditions.ToArray());

        _permissionDb.WritePermission(handle, permission);

        return permission;
    }

    IEnumerable<IPermission> CreatePreconditions(object handle, IPermissionCategory category, int level, int maxLevel)
    {
        if (level < maxLevel)
        {
            var numberOfPreconditions = TestContext.CurrentContext.Random.Next(0, 4);

            for (var i = 0; i < numberOfPreconditions; ++i)
            {
                var preconditions = CreatePreconditions(handle, category, level + 1, maxLevel);

                var permission = Permission.Random(category, preconditions.ToArray());

                _permissionDb.WritePermission(handle, permission);

                yield return permission;
            }
        }
    }

    IPermission CreatePermission(object handle, IPermissionCategory category, IEnumerable<IPermission> preconditions)
    {
        var permission = Permission.Random(category, preconditions);

        _permissionDb.WritePermission(handle, permission);

        return permission;
    }

    IPermissionCategory CreatePermissionCategory(object handle)
    {
        var application = CreateApplication(handle);
        var category = PermissionCategory.Random(application);

        _permissionDb.WritePermissionCategory(handle, category);

        return category;
    }

    IGroup CreateGroup(object handle)
    {
        var group = Group.Random();

        _groupDb.WriteGroup(handle, group);

        return group;
    }

    IApplication CreateApplication(object handle)
    {
        var application = new Application(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());

        var db = CreateApplicationDb();

        db.Write(handle, application);

        return application;
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

    static void AssertPermissionCategory(IPermissionCategory a, IPermissionCategory b)
    {
        Assert.AreEqual(a.Id, b.Id);
        Assert.AreEqual(a.Name, b.Name);
        AssertApplication(a.Application, b.Application);
    }

    static void AssertApplication(IApplication a, IApplication b)
    {
        Assert.AreEqual(a.Id, b.Id);
        Assert.AreEqual(a.Name, b.Name);
    }

    static void AssertPermission(IPermission a, IPermission b)
    {
        Assert.AreEqual(a.Id, b.Id);
        Assert.AreEqual(a.Name, b.Name);

        AssertPermissionCategory(a.Category, b.Category);

        var orderedPreconditionsA = a.Preconditions.OrderBy(p => p.Id).ToArray();
        var orderedPreconditionsB = b.Preconditions.OrderBy(p => p.Id).ToArray();

        Assert.AreEqual(orderedPreconditionsA.Length, orderedPreconditionsB.Length);

        for (var i = 0; i < orderedPreconditionsA.Length; ++i)
        {
            AssertPermission(orderedPreconditionsA[i], orderedPreconditionsB[i]);
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

    static void AssertGroup(IGroup a, IGroup b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Name, b.Name);
    }

    static void AssertSite(ISite a, ISite b)
    {
        Assert.AreEqual(a.Guid, b.Guid);
        Assert.AreEqual(a.Name, b.Name);
    }
}