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

public class Directory : IDirectory
{
    readonly IDb _db;
    readonly object _handle;

    internal Directory(IDb db, object handle, IDirectory directory)
        => (_db, _handle, Id, Name, ParentId) = (db, handle, directory.Id, directory.Name, directory.ParentId);

    public int Id { get; }

    int IDirectory.Id => Id;

    public string Name { get; private set; }

    string IDirectory.Name => Name;

    public Directory? GetParent()
    {
        if (ParentId.HasValue)
        {
            var parent = _db.Store.GetDirectory(_handle, ParentId.Value);

            return new Directory(_db, _handle, parent);
        }

        return null;
    }

    public int? ParentId { get; private set; }

    int? IDirectory.ParentId => ParentId;

    public IEnumerable<Directory> GetChildren()
    {
        ThrowIfNotExists();

        return _db.Store
            .GetDirectories(_handle, this)
            .Select(child => new Directory(_db, _handle, child))
            .OrderBy(child => child.Name.ToLower());
    }

    public Directory NewChild(string name)
    {
        var children = GetChildren();

        if (children.Any(child => child.Name == name))
        {
            throw new DirectoryAlreadyExistsException();
        }

        var child = _db.Store.NewDirectory(_handle, this, name);

        return new Directory(_db, _handle, child);
    }

    public void Rename(string newName)
    {
        var children = GetDirectoriesAtSameHierarchyLevel();

        if (children.Any(child => child.Id != Id && child.Name == newName))
        {
            throw new DirectoryAlreadyExistsException();
        }

        Name = newName;

        try
        {
            _db.Store.UpdateDirectory(_handle, this);
        }
        catch (Data.NotFoundException)
        {
            throw new DirectoryNotFoundException();
        }
    }

    IEnumerable<IDirectory> GetDirectoriesAtSameHierarchyLevel()
        => GetParent()?.GetChildren()
           ?? _db.Store.GetDirectories(_handle);

    public void Delete()
    {
        try
        {
            _db.Store.DeleteDirectory(_handle, this);
        }
        catch (Data.NotFoundException)
        {
            throw new DirectoryNotFoundException();
        }
    }

    public void MoveToRoot()
    {
        ThrowIfNotExists();

        if (GetParent() != null)
        {
            ThrowIfIsNameConflict(Name);

            ParentId = null;

            try
            {
                _db.Store.UpdateDirectory(_handle, this);
            }
            catch (Data.NotFoundException)
            {
                throw new DirectoryNotFoundException();
            }
        }
    }

    void ThrowIfIsNameConflict(string name)
    {
        var directories = _db.Store.GetDirectories(_handle);

        if (directories.Any(dir => dir.Name == name))
        {
            throw new DirectoryAlreadyExistsException();
        }
    }

    public void Move(Directory parent)
    {
        ThrowIfNotExists();
        ThrowIfNotExists(parent);

        if (parent.Id == Id)
        {
            throw new ArgumentException("Cannot move directory to itself.");
        }

        if (GetParent()?.Id != parent.Id)
        {
            ThrowIfIsNameConflict(parent, Name);
            ThrowIfIsChild(this, parent.Id);

            ParentId = parent.Id;

            _db.Store.UpdateDirectory(_handle, this);
        }
    }

    static void ThrowIfIsNameConflict(Directory parent, string name)
    {
        var children = parent.GetChildren();

        if (children.Any(child => child.Name == name))
        {
            throw new DirectoryAlreadyExistsException();
        }
    }

    void ThrowIfIsChild(IDirectory parent, int id)
    {
        var children = _db
            .Store
            .GetDirectories(_handle, parent)
            .ToArray();

        foreach (var child in children)
        {
            if (child.Id == id)
            {
                throw new ArgumentException("Cannot move directory to a subdirectory of itself.");
            }

            ThrowIfIsChild(child, id);
        }
    }

    public StoredMessageBuilder CreateMessageBuilder()
    {
        ThrowIfNotExists();

        var builder = new StoredMessageBuilder(_db, _handle);

        return builder.WithDirectory(this);
    }

    public IEnumerable<StoredMessage> GetMessages()
    {
        ThrowIfNotExists();

        var qb = new QueryBuilder()
            .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(Id));

        foreach (var directoryEntry in _db.Store.QueryMessages(_handle, qb.Build()))
        {
            yield return new StoredMessage(_db, _handle, directoryEntry);
        }
    }

    void ThrowIfNotExists()
        => ThrowIfNotExists(this);

    void ThrowIfNotExists(Directory directory)
    {
        try
        {
            _db.Store.GetDirectory(_handle, directory.Id);
        }
        catch (Data.NotFoundException)
        {
            throw new DirectoryNotFoundException($"Directory `{directory.Name}' (id={directory.Id}) not found.");
        }
    }
}