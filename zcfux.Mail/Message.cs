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
namespace zcfux.Mail;

public class Message
{
    readonly IMailDb _db;
    readonly object _handle;
    IMessage _message;

    public Message(IMailDb db, object handle, IMessage message)
    {
        _db = db;
        _handle = handle;
        _message = message;
    }

    public long Id
        => _message.Id;

    public Address From
        => _message.From;

    public IEnumerable<Address> To
        => _message.To;

    public IEnumerable<Address> Cc
        => _message.Cc;

    public IEnumerable<Address> Bcc
        => _message.Bcc;

    public string Subject
        => _message.Subject;

    public string? TextBody
        => _message.TextBody;

    public string? HtmlBody
        => _message.HtmlBody;

    public IEnumerable<Attachment> GetAttachments()
    {
        foreach (var attachment in _db.Attachments.Query(_handle, _message))
        {
            yield return new Attachment(_db, _handle, attachment);
        }
    }

    public void Move(Directory directory)
    {
        ThrowIfNotExists();
        
        if (!_db.Directories.Exists(_handle, directory.Id))
        {
            throw new DirectoryNotFoundException();
        }

        _message = _db.Messages.Move(_handle, _message, directory);
    }

    public void Delete()
    {
        ThrowIfNotExists();

        _db.Messages.Delete(_handle, Id);
    }

    void ThrowIfNotExists()
    {
        if (!_db.Messages.Exists(_handle, Id))
        {
            throw new MessageNotFoundException($"Message {Id}) not found.");
        }
    }
}