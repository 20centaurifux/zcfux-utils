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
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;
using zcfux.Mail.Queue;

namespace zcfux.Mail.LinqToPg.Queue;

internal sealed class QueueDb
{
    public IQueue NewQueue(Handle handle, string name)
    {
        var queue = new QueueRelation
        {
            Name = name
        };

        queue.Id = handle.Db().InsertWithInt32Identity(queue);

        return queue;
    }

    public void UpdateQueue(Handle handle, IQueue queue)
    {
        var updated = handle
            .Db()
            .GetTable<QueueRelation>()
            .Where(q => q.Id == queue.Id)
            .Set(q => q.Name, queue.Name)
            .Update();

        if (updated == 0)
        {
            throw new NotFoundException();
        }
    }

    public IQueue GetQueue(Handle handle, int id)
    {
        var queue = handle
            .Db()
            .GetTable<QueueRelation>()
            .SingleOrDefault(q => q.Id == id);

        if (queue == null)
        {
            throw new NotFoundException();
        }

        return queue;
    }

    public IEnumerable<IQueue> GetQueues(Handle handle)
        => handle
            .Db()
            .GetTable<QueueRelation>()
            .OrderBy(dir => dir.Name);

    public void DeleteQueue(Handle handle, IQueue queue)
    {
        var deleted = handle
            .Db()
            .GetTable<QueueRelation>()
            .Where(q => q.Id == queue.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IQueueItem NewQueueItem(Handle handle, IQueue queue, IMessage message, TimeSpan timeToLive, DateTime nextDue)
    {
        var queueItem = new QueueItemRelation(queue, message)
        {
            Created = DateTime.UtcNow,
            NextDue = nextDue
        };

        queueItem.EndOfLife = queueItem.Created.Add(timeToLive);

        handle.Db().Insert(queueItem);

        return queueItem;
    }

    public void UpdateQueueItem(Handle handle, IQueueItem queueItem)
    {
        var updated = handle
            .Db()
            .GetTable<QueueItemRelation>()
            .Where(item => item.QueueId == queueItem.Queue.Id && item.MessageId == queueItem.Message.Id)
            .Set(item => item.Created, queueItem.Created)
            .Set(item => item.EndOfLife, queueItem.EndOfLife)
            .Set(item => item.NextDue, queueItem.NextDue)
            .Set(item => item.Errors, queueItem.Errors)
            .Update();

        if (updated == 0)
        {
            throw new NotFoundException();
        }
    }

    public void DeleteQueueItem(Handle handle, IQueueItem queueItem)
    {
        var deleted = handle
            .Db()
            .GetTable<QueueItemRelation>()
            .Where(item => item.QueueId == queueItem.Queue.Id && item.MessageId == queueItem.Message.Id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IQueueItem> Query(Handle handle, Query query)
    {
        var useFlatView = FlatMessageViewTest.UseFlatView(query);

        return useFlatView
            ? QueryFlatView(handle, query)
            : QueryView(handle, query);
    }

    static IEnumerable<IQueueItem> QueryView(Handle handle, Query query)
    {
        var records = handle
            .Db()
            .GetTable<QueuedMessageView>()
            .Query(query);

        return records.Select(BuildQueueItem);
    }

    static IEnumerable<IQueueItem> QueryFlatView(Handle handle, Query query)
    {
        var db = handle.Db();

        IQueryable<FlatQueuedMessageView> q = db.GetTable<FlatQueuedMessageView>();

        if (query.HasFilter)
        {
            var expr = query.Filter!.ToExpression<FlatQueuedMessageView>();

            q = q.Where(expr);
        }

        var tempTable = q
            .Order(query.Order)
            .Select(flatMessage => new TemporaryFoundQueueItemRelation
            {
                QueueId = flatMessage.QueueId,
                Queue = flatMessage.Queue,
                MessageId = flatMessage.Id,
                Created = flatMessage.Created,
                EndOfLife = flatMessage.EndOfLife,
                NextDue = flatMessage.NextDue,
                Errors = flatMessage.Errors
            })
            .Distinct()
            .IntoTempTable();

        var records = tempTable
            .InnerJoin(db.GetTable<MessageRelation>(), (e, m) => e.MessageId == m.Id, (e, m) => new TemporaryQueuedMessageRelation
            {
                Id = m.Id!.Value,
                QueueId = e.QueueId,
                Queue = e.Queue,
                From = m.From,
                To = m.To,
                Cc = m.Cc,
                Bcc = m.Bcc,
                Subject = m.Subject,
                TextBody = m.TextBody,
                HtmlBody = m.HtmlBody,
                Created = e.Created,
                EndOfLife = e.EndOfLife,
                NextDue = e.NextDue,
                Errors = e.Errors,
                Offset = e.Offset
            })
            .OrderBy(msg => msg.Offset)
            .Range(query.Range);

        foreach (var record in records)
        {
            var queueItem = BuildQueueItem(record);

            yield return queueItem;
        }

        tempTable.Drop();
    }

    static IQueueItem BuildQueueItem(QueuedMessageView queuedMessage)
    {
        var queueItem = new QueueItem
        {
            Message = new Message
            {
                Id = queuedMessage.Id,
                From = Address.FromString(queuedMessage.From),
                To = queuedMessage.To.Select(Address.FromString),
                Cc = queuedMessage.Cc.Select(Address.FromString),
                Bcc = queuedMessage.Bcc.Select(Address.FromString),
                Subject = queuedMessage.Subject,
                TextBody = queuedMessage.TextBody,
                HtmlBody = queuedMessage.HtmlBody
            },
            Queue = new Queue
            {
                Id = queuedMessage.QueueId,
                Name = queuedMessage.Queue
            },
            Created = queuedMessage.Created,
            EndOfLife = queuedMessage.EndOfLife,
            NextDue = queuedMessage.NextDue,
            Errors = queuedMessage.Errors
        };

        return queueItem;
    }
}