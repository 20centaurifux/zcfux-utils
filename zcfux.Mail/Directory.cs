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

namespace zcfux.Mail;

public class Directory : IDirectory
{
    readonly IMailDb _db;
    readonly object _handle;

    internal Directory(IMailDb db, object handle, IDirectory directory)
    {
        (_db, _handle, Id, Name) = (db, handle, directory.Id, directory.Name);

        if (directory.Parent != null)
        {
            Parent = new Directory(db, _handle, directory.Parent);
        }
    }

    public int Id { get; }

    int IDirectory.Id => Id;

    public string Name { get; private set; }

    string IDirectory.Name => Name;

    public Directory? Parent { get; private set; }

    IDirectory? IDirectory.Parent => Parent;

    public IEnumerable<Directory> GetChildren()
    {
        ThrowIfNotExists();

        return _db.Directories
            .GetDirectories(_handle, this)
            .Select(child => new Directory(_db, _handle, child));
    }

    public Directory NewChild(string name)
    {
        ThrowIfNotExists();

        var children = GetChildren();

        if (children.Any(child => child.Name == name))
        {
            throw new DirectoryAlreadyExistsException();
        }

        var child = _db.Directories.NewDirectory(_handle, this, name);

        return new Directory(_db, _handle, child);
    }

    public void Rename(string newName)
    {
        ThrowIfNotExists();

        var children = GetDirectoriesAtSameHierarchyLevel();

        if (children.Any(child => child.Id != Id && child.Name == newName))
        {
            throw new DirectoryAlreadyExistsException();
        }

        Name = newName;

        _db.Directories.Update(_handle, this);
    }

    IEnumerable<IDirectory> GetDirectoriesAtSameHierarchyLevel()
        => Parent?.GetChildren()
           ?? _db.Directories.GetDirectories(_handle);

    public void Delete()
    {
        ThrowIfNotExists();

        _db.Directories.Delete(_handle, this);
    }

    public void MoveToRoot()
    {
        ThrowIfNotExists();

        if (Parent != null)
        {
            ThrowIfIsNameConflict(Name);

            Parent = null;

            _db.Directories.Update(_handle, this);
        }
    }

    void ThrowIfIsNameConflict(string name)
    {
        var directories = _db.Directories.GetDirectories(_handle);

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

        if (Parent?.Id != parent.Id)
        {
            ThrowIfIsNameConflict(parent, Name);
            ThrowIfIsChild(this, parent.Id);

            Parent = new Directory(_db, _handle, parent);

            _db.Directories.Update(_handle, this);
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
            .Directories
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

    public MessageBuilder CreateMessageBuilder()
    {
        ThrowIfNotExists();
        
        var builder = new MessageBuilder(_db, _handle);

        return builder.WithDirectory(this);
    }

    public IEnumerable<Message> GetMessages()
    {
        ThrowIfNotExists();

        var qb = new QueryBuilder()
            .WithFilter(MessageFilters.DirectoryId.EqualTo(Id));
        
        foreach (var message in _db.Messages.Query(_handle, qb.Build()))
        {
            yield return new Message(_db, _handle, message);
        }
    }

    void ThrowIfNotExists()
        => ThrowIfNotExists(this);

    void ThrowIfNotExists(Directory directory)
    {
        if (!_db.Directories.Exists(_handle, directory.Id))
        {
            throw new DirectoryNotFoundException($"Directory `{directory.Name}' (id={directory.Id}) not found.");
        }
    }
}