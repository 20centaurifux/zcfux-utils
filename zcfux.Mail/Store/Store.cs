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
using zcfux.Filter;

namespace zcfux.Mail.Store;

public sealed class Store
{
    readonly IDb _db;
    readonly object _handle;

    public Store(IDb db, object handle)
        => (_db, _handle) = (db, handle);

    public Directory NewDirectory(string name)
    {
        var directories = GetDirectories();

        if (directories.Any(child => child.Name == name))
        {
            throw new DirectoryAlreadyExistsException();
        }

        var directory = _db.Store.NewDirectory(_handle, name);

        return new Directory(_db, _handle, directory);
    }

    public IEnumerable<Directory> GetDirectories()
        => _db.Store.GetDirectories(_handle)
            .OrderBy(dir => dir.Name.ToLower())
            .Select(dir => new Directory(_db, _handle, dir));

    public IEnumerable<StoredMessage> Query(Query query)
        => _db.Store.QueryMessages(_handle, query)
            .Select(directoryEntry => new StoredMessage(_db, _handle, directoryEntry));
}