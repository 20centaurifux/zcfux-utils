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
using Npgsql;
using zcfux.Data;
using zcfux.Data.LinqToDB;

namespace zcfux.Mail.LinqToPg;

internal sealed class MessageDb : IMessageDb
{
    public IMessage NewMessage(
        object handle,
        Address from,
        IEnumerable<Address> to,
        IEnumerable<Address> cc,
        IEnumerable<Address> bcc,
        string subject,
        string? textBody,
        string? htmlBody)
    {
        var message = new MessageRelation
        {
            From = from.ToString(),
            To = to.Select(addr => addr.ToString()).ToArray(),
            Cc = cc.Select(addr => addr.ToString()).ToArray(),
            Bcc = bcc.Select(addr => addr.ToString()).ToArray(),
            Subject = subject,
            TextBody = textBody,
            HtmlBody = htmlBody
        };

        message.Id = handle.Db().InsertWithInt64Identity(message);

        return message;
    }

    public IMessage GetMessage(object handle, long id)
    {
        var message = handle
            .Db()
            .GetTable<MessageRelation>()
            .SingleOrDefault(msg => msg.Id == id);

        if (message == null)
        {
            throw new NotFoundException();
        }

        return message;
    }

    public void DeleteMessage(object handle, IMessage message)
    {
        var deleted = handle
            .Db()
            .GetTable<MessageRelation>()
            .Where(msg => msg.Id == message.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IAttachment NewAttachment(object handle, IMessage message, string filename, Stream stream)
    {
        var db = handle.Db();

        var attachment = new AttachmentRelation
        {
            MessageId = message.Id,
            Message = new MessageRelation(message),
            Filename = filename
        };

        var pgConnection = db.Connection as NpgsqlConnection;

        var manager = new NpgsqlLargeObjectManager(pgConnection!);

        attachment.Oid = manager.Create();

        using (var largeObject = manager.OpenReadWrite(attachment.Oid))
        {
            stream.CopyTo(largeObject);
        }

        attachment.Id = db.InsertWithInt64Identity(attachment);

        return attachment;
    }

    public IEnumerable<IAttachment> GetAttachments(object handle, IMessage message)
        => handle
            .Db()
            .GetTable<AttachmentRelation>()
            .Where(attachment => attachment.MessageId == message.Id);

    public Stream ReadAttachment(object handle, long id)
    {
        var db = handle.Db();

        var relation = db.GetTable<AttachmentRelation>().SingleOrDefault(attachment => attachment.Id == id);

        if (relation == null)
        {
            throw new NotFoundException();
        }

        var pgConnection = db.Connection as NpgsqlConnection;

        var manager = new NpgsqlLargeObjectManager(pgConnection!);

        return manager.OpenRead(relation.Oid);
    }

    public void DeleteAttachment(object handle, IAttachment attachment)
    {
        var deleted = handle
            .Db()
            .GetTable<AttachmentRelation>()
            .Where(attachment => attachment.Id == attachment.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }
}