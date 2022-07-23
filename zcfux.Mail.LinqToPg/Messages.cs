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
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.Mail.LinqToDB;

internal sealed class Messages
{
    sealed class ViewTest : IVisitor
    {
        public bool RequiresView { get; private set; }

        public void BeginFunction(string name)
        {
        }

        public void EndFunction()
        {
        }

        public void Visit(object? value)
        {
            if (value is IColumn column)
            {
                if (ColumnRequiresView(column.Name))
                {
                    RequiresView = true;
                }
            }
        }
    }

    static bool ColumnRequiresView(string columName)
        => (columName is "To" or "Cc" or "Bcc" or "Directory" or "AttachmentId" or "Attachment");

    public IMessage NewMessage(
        Handle handle,
        IDirectory directory,
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
            DirectoryId = directory.Id,
            Directory = new DirectoryRelation(directory),
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

    public IMessage Move(
        Handle handle,
        IMessage message,
        IDirectory directory)
    {
        var db = handle.Db();

        db.GetTable<MessageRelation>()
            .Where(msg => msg.Id == message.Id)
            .Set(msg => msg.DirectoryId, directory.Id)
            .Update();

        return db.GetTable<MessageRelation>()
            .First(msg => msg.Id == message.Id);
    }

    public IEnumerable<IMessage> Query(
        Handle handle,
        Query query)
    {
        var useView = UseView(query);

        return useView
            ? QueryView(handle, query)
            : QueryTable(handle, query);
    }

    static bool UseView(Query query)
    {
        var useView = false;

        if (query.HasFilter)
        {
            var visitor = new ViewTest();

            query.Filter?.Traverse(visitor);

            useView = visitor.RequiresView;
        }

        if (query.Order.Any(t => ColumnRequiresView(t.Item1)))
        {
            useView = true;
        }

        return useView;
    }

    static IEnumerable<IMessage> QueryView(Handle handle, Query query)
    {
        var db = handle.Db();

        var expr = query.Filter!.ToExpression<MessageView>();

        var ids = db
            .GetTable<MessageView>()
            .Where(expr)
            .Order(query.Order)
            .Select(msg => msg.Id)
            .Distinct()
            .Range(query.Range)
            .ToArray();

        var messages = db
            .GetTable<MessageRelation>()
            .Where(msg => ids.Contains(msg.Id!.Value))
            .ToDictionary(msg => msg.Id!.Value, msg => msg);

        foreach (var id in ids)
        {
            yield return messages[id];
        }
    }

    static IEnumerable<IMessage> QueryTable(Handle handle, Query query)
        => handle
            .Db()
            .GetTable<MessageRelation>()
            .Query(query);

    public bool Exists(
        Handle handle,
        long id)
        => handle
            .Db()
            .GetTable<MessageRelation>()
            .Any(msg => msg.Id == id);

    public void Delete(
        Handle handle,
        long id)
    {
        var db = handle.Db();

        var pgConnection = db.Connection as NpgsqlConnection;

        var manager = new NpgsqlLargeObjectManager(pgConnection!);

        foreach (var attachment in db.GetTable<AttachmentRelation>()
                     .Where(att => att.MessageId == id))
        {
            manager.Unlink(attachment.Oid);

            db.Delete(attachment);
        }

        db.GetTable<MessageRelation>()
            .Where(msg => msg.Id == id)
            .Delete();
    }
}