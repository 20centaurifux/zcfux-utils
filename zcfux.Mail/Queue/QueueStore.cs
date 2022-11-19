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
using zcfux.Data;
using zcfux.Filter;

namespace zcfux.Mail.Queue;

public sealed class QueueStore
{
    readonly IDb _db;
    readonly object _handle;

    public QueueStore(IDb db, object handle)
        => (_db, _handle) = (db, handle);

    public Queue NewQueue(string name)
    {
        var queue = _db.Queues.NewQueue(_handle, name);

        return new Queue(_db, _handle, queue);
    }

    public Queue GetQueue(int id)
    {
        try
        {
            var queue = _db.Queues.GetQueue(_handle, id);

            return new Queue(_db, _handle, queue);
        }
        catch (NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }

    public IEnumerable<Queue> GetQueues()
        => _db.Queues.GetQueues(_handle)
            .OrderBy(queue => queue.Name.ToLower())
            .Select(queue => new Queue(_db, _handle, queue));

    public IEnumerable<QueuedMessage> Query(Query query)
        => _db.Queues.Query(_handle, query)
            .Select(queueItem => new QueuedMessage(_db, _handle, queueItem));
}