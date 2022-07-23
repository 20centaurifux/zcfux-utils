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
using zcfux.Data.LinqToDB;

namespace zcfux.Mail.LinqToDB;

internal sealed class Directories
{
    public IDirectory NewDirectory(Handle handle, string name)
    {
        var directory = new DirectoryRelation
        {
            Name = name
        };

        directory.Id = handle.Db().InsertWithInt32Identity(directory);

        return directory;
    }

    public IDirectory NewDirectory(Handle handle, IDirectory parent, string name)
    {
        var directory = new DirectoryRelation
        {
            ParentId = parent.Id,
            Name = name
        };

        var id = handle.Db().InsertWithInt32Identity(directory);

        return handle
            .Db()
            .GetTable<DirectoryRelation>()
            .LoadWith(dir => dir.Parent)
            .Single(dir => dir.Id == id);
    }

    public IEnumerable<IDirectory> GetDirectories(Handle handle)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => !dir.ParentId.HasValue)
            .OrderBy(dir => dir.Name);

    public IEnumerable<IDirectory> GetDirectories(Handle handle, IDirectory parent)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .LoadWith(dir => dir.Parent)
            .Where(dir => dir.ParentId == parent.Id)
            .OrderBy(dir => dir.Name);

    public bool Exists(Handle handle, int id)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Any(dir => dir.Id == id);

    public void Update(Handle handle, IDirectory directory)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => dir.Id == directory.Id)
            .Set(dir => dir.Name, directory.Name)
            .Set(dir => dir.ParentId, directory.Parent?.Id)
            .Update();

    public void Delete(Handle handle, IDirectory directory)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => dir.Id == directory.Id)
            .Delete();
}