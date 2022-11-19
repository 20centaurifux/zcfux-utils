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
using LinqToDB.Data;
using zcfux.Application;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.User.LinqToDB;

public sealed class GroupDb : IGroupDb
{
    readonly ISiteDb _siteDb;

    public GroupDb(ISiteDb siteDb)
        => _siteDb = siteDb;

    public void WriteGroup(object handle, IGroup group)
    {
        var db = handle.Db();

        if (db
            .GetTable<GroupRelation>()
            .Any(g => !g.Guid.Equals(group.Guid) && g.Name.Equals(group.Name)))
        {
            throw new ConflictException();
        }

        var relation = new GroupRelation(group);

        db.InsertOrReplace(relation);
    }

    public IEnumerable<IGroup> QueryGroups(object handle, Query query)
        => handle
            .Db()
            .GetTable<GroupRelation>()
            .Query(query);

    public void DeleteGroup(object handle, Guid guid)
    {
        var deleted = handle
            .Db()
            .GetTable<GroupRelation>()
            .Where(s => s.Guid == guid)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public void AssignUser(object handle, ISite site, IGroup group, IUser user)
    {
        ThrowIfSiteNotFound(handle, site.Guid);

        var db = handle.Db();

        ThrowIfGroupNotFound(db, group.Guid);
        ThrowIfUserNotFound(db, user.Guid);
        ThrowIfUserIsAlreadyAssigned(db, site.Guid, group.Guid, user.Guid);

        if (db
            .GetTable<AssignedUserRelation>()
            .Any(a =>
                a.Site.Equals(site.Guid)
                && a.Group.Equals(group.Guid)
                && a.User.Equals(user.Guid)))
        {
            throw new ConflictException();
        }

        var relation = new AssignedUserRelation
        {
            Site = site.Guid,
            Group = group.Guid,
            User = user.Guid
        };

        db.Insert(relation);
    }

    void ThrowIfSiteNotFound(object handle, Guid guid)
    {
        var qb = new QueryBuilder()
            .WithFilter(SiteFilters.Guid.EqualTo(guid));

        if (!_siteDb.Query(handle, qb.Build()).Any())
        {
            throw new NotFoundException("Site not found.");
        }
    }

    static void ThrowIfGroupNotFound(DataConnection db, Guid guid)
    {
        if (!db
            .GetTable<GroupRelation>()
            .Any(g => g.Guid.Equals(guid)))
        {
            throw new NotFoundException("Group not found.");
        }
    }

    static void ThrowIfUserNotFound(DataConnection db, Guid guid)
    {
        if (!db
            .GetTable<UserRelation>()
            .Any(u => u.Guid.Equals(guid)))
        {
            throw new NotFoundException("User not found.");
        }
    }

    static void ThrowIfUserIsAlreadyAssigned(DataConnection db, Guid site, Guid group, Guid user)
    {
        if (db
            .GetTable<AssignedUserRelation>()
            .Any(a =>
                    a.Site.Equals(site)
                    && a.Group.Equals(group)
                    && a.User.Equals(user)))
        {
            throw new ConflictException();
        }
    }

    public void UnassignUser(object handle, ISite site, IGroup group, IUser user)
    {
        var deleted = handle
            .Db()
            .GetTable<AssignedUserRelation>()
            .Where(a => a.Site == site.Guid && a.Group == group.Guid && a.User == user.Guid)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IAssignedUser> QueryAssignedUsers(object handle, Query query)
        => handle
            .Db()
            .GetTable<AssignedUserView>()
            .Query(query);
}