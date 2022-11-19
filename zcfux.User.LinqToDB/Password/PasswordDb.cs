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
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.User.Password;

namespace zcfux.User.LinqToDB.Password
{
    public sealed class PasswordDb : IPasswordDb
    {
        public void Set(object handle, Guid guid, byte[] hash, byte[] salt, int format)
        {
            if (!handle
                    .Db()
                    .GetTable<UserRelation>()
                    .Any(u => u.Guid == guid))
            {
                throw new NotFoundException();
            }

            var relation = new PasswordRelation
            {
                User = guid,
                Hash = hash,
                Salt = salt,
                Format = format
            };

            handle.Db().InsertOrReplace(relation);
        }

        public IPassword? TryGet(object handle, Guid guid)
            => handle
                .Db()
                .GetTable<PasswordRelation>()
                .SingleOrDefault(p => p.User == guid);

        public void Delete(object handle, Guid guid)
        {
            var deleted = handle
                .Db()
                .GetTable<PasswordRelation>()
                .Where(p => p.User == guid)
                .Delete();

            if (deleted == 0)
            {
                throw new NotFoundException();
            }
        }
    }
}