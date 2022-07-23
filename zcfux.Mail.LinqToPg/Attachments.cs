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
using zcfux.Data.LinqToDB;

namespace zcfux.Mail.LinqToDB;

internal sealed class Attachments
{
    public IAttachment NewAttachment(
        Handle handle,
        IMessage message,
        string filename,
        Stream stream)
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

    public IEnumerable<IAttachment> Query(
        Handle handle,
        IMessage message)
        => handle
            .Db()
            .GetTable<AttachmentRelation>()
            .Where(attachment => attachment.MessageId == message.Id);

    public Stream OpenRead(
        Handle handle,
        long id)
    {
        var db = handle.Db();

        var relation = db.GetTable<AttachmentRelation>().Single(attachment => attachment.Id == id);

        var pgConnection = db.Connection as NpgsqlConnection;

        var manager = new NpgsqlLargeObjectManager(pgConnection!);

        return manager.OpenRead(relation.Oid);
    }

    public void Delete(
        Handle handle,
        long id)
    {
        var db = handle.Db();

        var relation = db.GetTable<AttachmentRelation>().Single(attachment => attachment.Id == id);

        var pgConnection = db.Connection as NpgsqlConnection;

        var manager = new NpgsqlLargeObjectManager(pgConnection!);

        manager.Unlink(relation.Oid);

        db.Delete(relation);
    }
}