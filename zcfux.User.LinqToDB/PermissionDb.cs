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
using System.Collections.Immutable;
using LinqToDB;
using LinqToDB.Data;
using zcfux.Application;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.User.LinqToDB;

public sealed class PermissionDb : IPermissionDb
{
    readonly IApplicationDb _applicationDb;

    public PermissionDb(IApplicationDb applicationDb)
        => _applicationDb = applicationDb;

    sealed record Vertex(int Id, string Name, IPermissionCategory Category);

    sealed record Group(Guid Guid, string Name) : IGroup;

    sealed record PermissionCategory(int Id, string Name, IApplication Application) : IPermissionCategory;

    sealed record GrantedPermission(IGroup Group, IPermission Permission) : IGrantedPermission;

    public void WritePermissionCategory(object handle, IPermissionCategory category)
    {
        ThrowIfApplicationNotFound(handle, category.Application.Id);

        var db = handle.Db();

        if (db
            .GetTable<PermissionCategoryRelation>()
            .Any(c =>
                !c.Id.Equals(category.Id)
                && c.Name.Equals(category.Name)
                && c.ApplicationId.Equals(category.Application.Id)))
        {
            throw new ConflictException();
        }

        var relation = new PermissionCategoryRelation(category);

        db.InsertOrReplace(relation);
    }

    void ThrowIfApplicationNotFound(object handle, int id)
    {
        var qb = new QueryBuilder()
            .WithFilter(ApplicationFilters.Id.EqualTo(id));

        if (!_applicationDb.Query(handle, qb.Build()).Any())
        {
            throw new NotFoundException("Application not found.");
        }
    }

    public void DeletePermissionCategory(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<PermissionCategoryRelation>()
            .Where(c => c.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IPermissionCategory> QueryPermissionCategories(object handle, Query query)
        => handle
            .Db()
            .GetTable<PermissionCategoryView>()
            .Query(query)
            .Select(c => c.ToPermissionCategory());

    public void WritePermission(object handle, IPermission permission)
    {
        var db = handle.Db();

        ThrowIfPermissionCategoryNotFound(db, permission.Category.Id);

        if (db
            .GetTable<PermissionRelation>()
            .Any(p =>
                !p.Id.Equals(permission.Id)
                && p.CategoryId.Equals(permission.Category.Id)
                && p.Name.Equals(permission.Name)))
        {
            throw new ConflictException();
        }

        var relation = new PermissionRelation(permission);

        db.InsertOrReplace(relation);

        db
            .GetTable<PermissionDependencyRelation>()
            .Where(d => d.PermissionId.Equals(permission.Id))
            .Delete();

        foreach (var precondition in permission.Preconditions)
        {
            var dependencyRelation = new PermissionDependencyRelation
            {
                PermissionId = permission.Id,
                DependencyId = precondition.Id
            };

            db.Insert(dependencyRelation);
        }
    }

    static void ThrowIfPermissionCategoryNotFound(DataConnection db, int id)
    {
        if (!db
                .GetTable<PermissionCategoryRelation>()
                .Any(c => c.Id.Equals(id)))
        {
            throw new NotFoundException("Permission category not found.");
        }
    }

    public void DeletePermission(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<PermissionRelation>()
            .Where(p => p.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IPermission> QueryPermissions(object handle, Query query)
    {
        var edges = GetEdges(handle.Db()).ToArray();

        var permissions = handle
            .Db()
            .GetTable<PermissionView>()
            .Query(query)
            .Select(c => c.ToPermission());

        foreach (var permission in permissions)
        {
            AddDependencies(
                permission,
                edges,
                ImmutableHashSet.Create(permission.Id));

            yield return permission;
        }
    }

    public void Grant(object handle, IPermission permission, IGroup group)
    {
        var db = handle.Db();

        if (db.GetTable<GrantedPermissionRelation>().Any(g =>
                g.PermissionId.Equals(permission.Id) && g.Group.Equals(group.Guid)))
        {
            throw new ConflictException();
        }

        ThrowIfParentPermissionsNotGranted(db, permission.Id, group.Guid);

        var relation = new GrantedPermissionRelation
        {
            PermissionId = permission.Id,
            Group = group.Guid
        };

        db.Insert(relation);
    }

    void ThrowIfParentPermissionsNotGranted(DataConnection db, int permissionId, Guid group)
    {
        var query = GrantedParentPermissionsCte(db, permissionId, group);

        if (query.Any(g => !g.Granted))
        {
            throw new InvalidOperationException("Requirement not fulfilled.");
        }
    }

    static IQueryable<GrantedPermissionCte> GrantedParentPermissionsCte(DataConnection db, int permissionId, Guid groupUid)
    {
        var queryable = db.GetCte<GrantedPermissionCte>(parent =>
        {
            return (
                    from d in db.GetTable<PermissionDependencyRelation>()
                    from g in db.GetTable<GrantedPermissionRelation>()
                        .LeftJoin(p =>
                            p.PermissionId.Equals(d.DependencyId)
                            && p.Group.Equals(groupUid))
                    where d.PermissionId.Equals(permissionId)
                    select new GrantedPermissionCte
                    {
                        PermissionId = d.DependencyId,
                        Granted = (g != null)
                    }
                )
                .Concat(
                    from child in parent
                    from d in db.GetTable<PermissionDependencyRelation>()
                        .InnerJoin(p => p.PermissionId.Equals(child.PermissionId))
                    from g in db.GetTable<GrantedPermissionRelation>()
                        .LeftJoin(p =>
                            p.PermissionId.Equals(d.DependencyId)
                            && p.Group.Equals(groupUid))
                    select new GrantedPermissionCte
                    {
                        PermissionId = d.DependencyId,
                        Granted = (g != null)
                    }
                );
        });

        return queryable;
    }

    public void Revoke(object handle, IPermission permission, IGroup group)
    {
        var db = handle.Db();

        ThrowIfChildPermissionsNotGranted(db, permission.Id, group.Guid);

        var deleted = db
            .GetTable<GrantedPermissionRelation>()
            .Where(g =>
                g.PermissionId.Equals(permission.Id) && g.Group.Equals(group.Guid))
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    void ThrowIfChildPermissionsNotGranted(DataConnection db, int permissionId, Guid group)
    {
        var query = GrantedChildPermissionsCte(db, permissionId, group);

        if (query.Any(g => g.Granted))
        {
            throw new InvalidOperationException("Granted child permission found.");
        }
    }

    static IQueryable<GrantedPermissionCte> GrantedChildPermissionsCte(DataConnection db, int permissionId, Guid groupUid)
    {
        var queryable = db.GetCte<GrantedPermissionCte>(parent =>
        {
            return (
                    from d in db.GetTable<PermissionDependencyRelation>()
                    from g in db.GetTable<GrantedPermissionRelation>()
                        .LeftJoin(p =>
                            p.PermissionId.Equals(d.PermissionId)
                            && p.Group.Equals(groupUid))
                    where d.DependencyId.Equals(permissionId)
                    select new GrantedPermissionCte
                    {
                        PermissionId = d.PermissionId,
                        Granted = (g != null)
                    }
                )
                .Concat(
                    from child in parent
                    from d in db.GetTable<PermissionDependencyRelation>()
                        .InnerJoin(p => p.DependencyId.Equals(child.PermissionId))
                    from g in db.GetTable<GrantedPermissionRelation>()
                        .LeftJoin(p =>
                            p.PermissionId.Equals(d.PermissionId)
                            && p.Group.Equals(groupUid))
                    select new GrantedPermissionCte
                    {
                        PermissionId = d.PermissionId,
                        Granted = (g != null)
                    }
                );
        });

        return queryable;
    }

    public IEnumerable<IGrantedPermission> QueryGrantedPermissions(object handle, Query query)
    {
        var groupCache = new Dictionary<Guid, Group>();
        var permissionCache = new Dictionary<int, Permission>();
        var permissionCategoryCache = new Dictionary<int, PermissionCategory>();
        var applicationCache = new Dictionary<int, Application>();

        var edges = GetEdges(handle.Db()).ToArray();

        var grantedPermissions = handle
            .Db()
            .GetTable<GrantedPermissionView>()
            .Query(query);

        foreach (var grantedPermission in grantedPermissions)
        {
            if (!groupCache.TryGetValue(grantedPermission.GroupUid, out var group))
            {
                group = new Group(grantedPermission.GroupUid, grantedPermission.Group);

                groupCache.Add(group.Guid, group);
            }

            if (!permissionCache.TryGetValue(grantedPermission.Id, out var permission))
            {
                if (!permissionCategoryCache.TryGetValue(grantedPermission.CategoryId, out var permissionCategory))
                {
                    if (!applicationCache.TryGetValue(grantedPermission.ApplicationId, out var application))
                    {
                        application = new Application(grantedPermission.ApplicationId, grantedPermission.Application);

                        applicationCache.Add(application.Id, application);
                    }

                    permissionCategory = new PermissionCategory(grantedPermission.CategoryId, grantedPermission.Category, application);

                    permissionCategoryCache.Add(permissionCategory.Id, permissionCategory);
                }

                permission = new Permission(grantedPermission.Id, grantedPermission.Name, permissionCategory);

                AddDependencies(
                    permission,
                    edges,
                    ImmutableHashSet.Create(permission.Id));

                permissionCache.Add(permission.Id, permission);
            }

            yield return new GrantedPermission(group, permission);
        }
    }

    static void AddDependencies(Permission permission, KeyValuePair<Vertex, Vertex>[] edges, ImmutableHashSet<int> visited)
    {
        var dependencies = edges
            .Where(e => e.Key.Id == permission.Id)
            .Select(e => e.Value)
            .ToArray();

        foreach (var dependency in dependencies)
        {
            if (visited.Contains(dependency.Id))
            {
                throw new InvalidOperationException("Loop detected.");
            }

            var precondition = new Permission(
                dependency.Id,
                dependency.Name,
                dependency.Category);

            AddDependencies(precondition, edges, visited.Add(precondition.Id));

            permission.AddPrecondition(precondition);
        }
    }

    static IEnumerable<KeyValuePair<Vertex, Vertex>> GetEdges(DataConnection db)
    {
        var cache = new Dictionary<int, Vertex>();

        foreach (var edge in db.GetTable<PermissionDependencyView>())
        {
            if (!cache.TryGetValue(edge.LeftId, out var permission))
            {
                permission = new Vertex(
                    edge.LeftId,
                    edge.LeftPermission,
                    new PermissionCategory(
                        edge.LeftCategoryId,
                        edge.LeftCategory,
                        new Application(
                            edge.LeftApplicationId,
                            edge.LeftApplication)));

                cache.Add(permission.Id, permission);
            }

            if (!cache.TryGetValue(edge.RightId, out var dependency))
            {
                dependency = new Vertex(
                    edge.RightId,
                    edge.RightPermission,
                    new PermissionCategory(
                        edge.RightCategoryId,
                        edge.RightCategory,
                        new Application(
                            edge.LeftApplicationId,
                            edge.LeftApplication)));

                cache.Add(dependency.Id, dependency);
            }

            yield return new KeyValuePair<Vertex, Vertex>(permission, dependency);
        }
    }
}