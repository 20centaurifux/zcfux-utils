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
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.User.LinqToDB;

public sealed class UserDb : IUserDb
{
    public void WriteOrigin(object handle, IOrigin origin)
    {
        var db = handle.Db();

        ThrowIfOriginNameInUse(db, origin);

        var relation = new OriginRelation(origin);

        db.InsertOrReplace(relation);
    }

    static void ThrowIfOriginNameInUse(DataConnection db, IOrigin origin)
    {
        if (db
            .GetTable<OriginRelation>()
            .Any(o =>
                !o.Id.Equals(origin.Id)
                && o.Name.Equals(origin.Name)))
        {
            throw new ConflictException();
        }
    }

    public void DeleteOrigin(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<OriginRelation>()
            .Where(s => s.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IOrigin> QueryOrigins(object handle, Query query)
        => handle
            .Db()
            .GetTable<OriginRelation>()
            .Query(query);

    public void WriteUser(object handle, IUser user)
    {
        var db = handle.Db();

        ThrowIfOriginNotFound(db, user.Origin.Id);
        ThrowIfUsernameInUse(db, user);

        var relation = new UserRelation(user);

        db.InsertOrReplace(relation);
    }

    static void ThrowIfOriginNotFound(DataConnection db, int id)
    {
        if (!db
                .GetTable<OriginRelation>()
                .Any(o => o.Id.Equals(id)))
        {
            throw new NotFoundException("Origin not found.");
        }
    }

    static void ThrowIfUsernameInUse(DataConnection db, IUser user)
    {
        if (db
            .GetTable<UserRelation>()
            .Any(u =>
                !u.Guid.Equals(user.Guid)
                && u.Name.Equals(user.Name)
                && u.OriginId.Equals(user.Origin.Id)))
        {
            throw new ConflictException();
        }
    }

    public void DeleteUser(object handle, Guid guid)
    {
        var deleted = handle
            .Db()
            .GetTable<UserRelation>()
            .Where(u => u.Guid == guid)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IUser> QueryUsers(object handle, Query query)
        => handle
            .Db()
            .GetTable<UserView>()
            .Query(query)
            .Select(v => v.ToUser());
}