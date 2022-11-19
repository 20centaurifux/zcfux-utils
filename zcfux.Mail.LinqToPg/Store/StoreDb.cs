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
using zcfux.Mail.Store;

namespace zcfux.Mail.LinqToPg.Store;

internal sealed class StoreDb : IStoreDb
{
    public IDirectory NewDirectory(object handle, string name)
    {
        var directory = new DirectoryRelation
        {
            Name = name
        };

        directory.Id = handle.Db().InsertWithInt32Identity(directory);

        return directory;
    }

    public IDirectory NewDirectory(object handle, IDirectory parent, string name)
    {
        var directory = new DirectoryRelation
        {
            ParentId = parent.Id,
            Name = name
        };

        directory.Id = handle.Db().InsertWithInt32Identity(directory);

        return directory;
    }

    public void UpdateDirectory(object handle, IDirectory directory)
    {
        var updated = handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => dir.Id == directory.Id)
            .Set(dir => dir.Name, directory.Name)
            .Set(dir => dir.ParentId, directory.ParentId)
            .Update();

        if (updated == 0)
        {
            throw new NotFoundException();
        }
    }

    public IDirectory GetDirectory(object handle, int id)
    {
        var directory = handle
            .Db()
            .GetTable<DirectoryRelation>()
            .SingleOrDefault(dir => dir.Id == id);

        if (directory == null)
        {
            throw new NotFoundException();
        }

        return directory;
    }

    public IEnumerable<IDirectory> GetDirectories(object handle)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => !dir.ParentId.HasValue)
            .OrderBy(dir => dir.Name);

    public IEnumerable<IDirectory> GetDirectories(object handle, IDirectory parent)
        => handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => dir.ParentId == parent.Id)
            .OrderBy(dir => dir.Name);

    public void DeleteDirectory(object handle, IDirectory directory)
    {
        var deleted = handle
            .Db()
            .GetTable<DirectoryRelation>()
            .Where(dir => dir.Id == directory.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IDirectoryEntry LinkMessage(object handle, IMessage message, IDirectory directory)
    {
        var db = handle.Db();

        if (db.GetTable<DirectoryEntryRelation>()
            .Any(entry => entry.MessageId == message.Id && entry.DirectoryId == directory.Id))
        {
            throw new ConflictException();
        }

        var directoryEntry = new DirectoryEntryRelation(message, directory);

        handle.Db().Insert(directoryEntry);

        return directoryEntry;
    }

    public void UnlinkMessage(object handle, IDirectoryEntry directoryEntry)
    {
        var deleted = handle
            .Db()
            .GetTable<DirectoryEntryRelation>()
            .Where(entry => entry.MessageId == directoryEntry.Message.Id && entry.DirectoryId == directoryEntry.Directory.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IDirectoryEntry MoveMessage(object handle, IMessage message, IDirectory from, IDirectory to)
    {
        var updated = handle
            .Db()
            .GetTable<DirectoryEntryRelation>()
            .Where(entry => entry.MessageId == message.Id && entry.DirectoryId == from.Id)
            .Set(entry => entry.DirectoryId, to.Id)
            .Update();

        if (updated == 0)
        {
            throw new NotFoundException();
        }

        return new DirectoryEntryRelation(message, to);
    }

    public IEnumerable<IDirectoryEntry> QueryMessages(object handle, Query query)
    {
        var useFlatView = FlatMessageViewTest.UseFlatView(query);

        return useFlatView
            ? QueryFlatView(handle, query)
            : QueryView(handle, query);
    }

    static IEnumerable<IDirectoryEntry> QueryView(object handle, Query query)
    {
        var records = handle
            .Db()
            .GetTable<StoredMessageView>()
            .Query(query);

        foreach (var record in records)
        {
            var directoryEntry = BuildDirectoryEntry(record);

            yield return directoryEntry;
        }
    }

    static IEnumerable<IDirectoryEntry> QueryFlatView(object handle, Query query)
    {
        var db = handle.Db();

        IQueryable<FlatStoredMessageView> q = db.GetTable<FlatStoredMessageView>();

        if (query.HasFilter)
        {
            var expr = query.Filter!.ToExpression<FlatStoredMessageView>();

            q = q.Where(expr);
        }

        var tempTable = q
            .Order(query.Order)
            .Select(flatMessage => new TemporaryDirectoryEntryRelation
            {
                DirectoryId = flatMessage.DirectoryId,
                Directory = flatMessage.Directory,
                ParentId = flatMessage.ParentId,
                MessageId = flatMessage.Id
            })
            .Distinct()
            .IntoTempTable();

        var records = tempTable
            .InnerJoin(db.GetTable<MessageRelation>(), (e, m) => e.MessageId == m.Id, (e, m) => new TemporaryStoredMessageRelation
            {
                Id = m.Id!.Value,
                DirectoryId = e.DirectoryId,
                Directory = e.Directory,
                ParentId = e.ParentId,
                From = m.From,
                To = m.To,
                Cc = m.Cc,
                Bcc = m.Bcc,
                Subject = m.Subject,
                TextBody = m.TextBody,
                HtmlBody = m.HtmlBody,
                Offset = e.Offset
            })
            .OrderBy(msg => msg.Offset)
            .Range(query.Range);

        foreach (var record in records)
        {
            var directoryEntry = BuildDirectoryEntry(record);

            yield return directoryEntry;
        }

        tempTable.Drop();
    }

    static IDirectoryEntry BuildDirectoryEntry(StoredMessageView storedMessage)
    {
        var directoryEntry = new DirectoryEntry
        {
            Message = new Message
            {
                Id = storedMessage.Id,
                From = Address.FromString(storedMessage.From),
                To = storedMessage.To.Select(Address.FromString),
                Cc = storedMessage.Cc.Select(Address.FromString),
                Bcc = storedMessage.Bcc.Select(Address.FromString),
                Subject = storedMessage.Subject,
                TextBody = storedMessage.TextBody,
                HtmlBody = storedMessage.HtmlBody
            },
            Directory = new Directory
            {
                Id = storedMessage.DirectoryId,
                Name = storedMessage.Directory,
                ParentId = storedMessage.ParentId
            }
        };

        return directoryEntry;
    }
}