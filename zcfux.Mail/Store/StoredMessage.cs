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
namespace zcfux.Mail.Store;

public sealed class StoredMessage : IEmail
{
    readonly IDb _db;
    readonly object _handle;
    IDirectoryEntry _directoryEntry;

    public StoredMessage(IDb db, object handle, IDirectoryEntry directoryEntry)
        => (_db, _handle, _directoryEntry) = (db, handle, directoryEntry);

    public long Id
        => _directoryEntry.Message.Id;

    public Address From
        => _directoryEntry.Message.From;

    public IEnumerable<Address> To
        => _directoryEntry.Message.To;

    public IEnumerable<Address> Cc
        => _directoryEntry.Message.Cc;

    public IEnumerable<Address> Bcc
        => _directoryEntry.Message.Bcc;

    public string Subject
        => _directoryEntry.Message.Subject;

    public string? TextBody
        => _directoryEntry.Message.TextBody;

    public string? HtmlBody
        => _directoryEntry.Message.HtmlBody;

    public IEnumerable<Attachment> GetAttachments()
    {
        foreach (var attachment in _db.Messages.GetAttachments(_handle, _directoryEntry.Message))
        {
            yield return new Attachment(_db, _handle, attachment);
        }
    }

    public Directory Directory
        => new(_db, _handle, _directoryEntry.Directory);

    public void Move(Directory directory)
    {
        if (_directoryEntry.Directory.Id != directory.Id)
        {
            IDirectory from;

            try
            {
                from = _db.Store.GetDirectory(_handle, _directoryEntry.Directory.Id);
            }
            catch (Data.NotFoundException)
            {
                throw new DirectoryNotFoundException($"Directory (id={_directoryEntry.Directory.Id}) not found.");
            }

            IDirectory to;

            try
            {
                to = _db.Store.GetDirectory(_handle, directory.Id);
            }
            catch (Data.NotFoundException)
            {
                throw new DirectoryNotFoundException($"Directory (id={directory.Id}) not found.");
            }

            try
            {
                _directoryEntry = _db.Store.MoveMessage(_handle, _directoryEntry.Message, from, to);
            }
            catch (Data.NotFoundException)
            {
                throw new DirectoryEntryNotFoundException();
            }
        }
    }

    public void Delete()
    {
        try
        {
            _db.Store.UnlinkMessage(_handle, _directoryEntry);
        }
        catch(Data.NotFoundException)
        {
            throw new DirectoryEntryNotFoundException();
        }

        try
        {
            _db.Messages.DeleteMessage(_handle, _directoryEntry.Message);
        }
        catch(Data.NotFoundException)
        {
            throw new MessageNotFoundException();
        }
    }
}