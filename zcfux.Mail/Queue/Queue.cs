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

namespace zcfux.Mail.Queue;

public sealed class Queue : IQueue
{
    readonly IDb _db;
    readonly object _handle;

    public Queue(IDb db, object handle, IQueue queue)
        => (_db, _handle, Id, Name) = (db, handle, queue.Id, queue.Name);

    public int Id { get; private set; }

    int IQueue.Id
        => Id;

    public string Name { get; private set; }

    string IQueue.Name
        => Name;

    public void Rename(string newName)
    {
        Name = newName;

        try
        {
            _db.Queues.UpdateQueue(_handle, this);
        }
        catch (Data.NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }

    public QueuedMessageBuilder CreateMessageBuilder()
    {
        try
        {
            var queue = _db.Queues.GetQueue(_handle, Id);

            var builder = new QueuedMessageBuilder(_db, _handle);

            return builder.WithQueue(queue);
        }
        catch(Data.NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }

    public bool TryGetNext(out QueuedMessage? queuedMessage)
    {
        try
        {
            var success = false;

            var queue = _db.Queues.GetQueue(_handle, Id);

            queuedMessage = null;

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.QueueId.EqualTo(queue.Id))
                .WithOrderBy(QueuedMessageFilters.NextDue)
                .WithLimit(1);

            var queueItem = _db.Queues.Query(_handle, qb.Build())
                .SingleOrDefault();

            if (queueItem != null)
            {
                queuedMessage = new QueuedMessage(_db, _handle, queueItem);

                success = true;
            }

            return success;
        }
        catch (Data.NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }

    public void Delete()
    {
        try
        {
            _db.Queues.DeleteQueue(_handle, this);
        }
        catch (Data.NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }
}