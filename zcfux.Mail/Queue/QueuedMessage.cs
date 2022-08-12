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

public sealed class QueuedMessage
{
    sealed class QueueItem : IQueueItem
    {
        public QueueItem(IQueueItem queueItem)
        {
            Queue = queueItem.Queue;
            Message = queueItem.Message;
            Created = queueItem.Created;
            EndOfLife = queueItem.EndOfLife;
            NextDue = queueItem.NextDue;
            Errors = queueItem.Errors;
        }

        public IQueue Queue { get; set; }

        public IMessage Message { get; set; }

        public DateTime Created { get; set; }

        public DateTime EndOfLife { get; set; }

        public DateTime? NextDue { get; set; }

        public int Errors { get; set; }
    }

    readonly IDb _db;
    readonly object _handle;
    readonly QueueItem _queueItem;

    public QueuedMessage(IDb db, object handle, IQueueItem queueItem)
        => (_db, _handle, _queueItem) = (db, handle, new QueueItem(queueItem));

    public long Id
        => _queueItem.Message.Id;

    public Address From
        => _queueItem.Message.From;

    public IEnumerable<Address> To
        => _queueItem.Message.To;

    public IEnumerable<Address> Cc
        => _queueItem.Message.Cc;

    public IEnumerable<Address> Bcc
        => _queueItem.Message.Bcc;

    public string Subject
        => _queueItem.Message.Subject;

    public string? TextBody
        => _queueItem.Message.TextBody;

    public string? HtmlBody
        => _queueItem.Message.HtmlBody;

    public IEnumerable<Attachment> GetAttachments()
    {
        foreach (var attachment in _db.Messages.GetAttachments(_handle, _queueItem.Message))
        {
            yield return new Attachment(_db, _handle, attachment);
        }
    }
    
    public DateTime Created
        => _queueItem.Created;

    public DateTime EndOfLife
        => _queueItem.EndOfLife;

    public DateTime? NextDue
        => _queueItem.NextDue;

    public int Errors
        => _queueItem.Errors;

    public void Fail(TimeSpan retry)
    {
        ++_queueItem.Errors;
        _queueItem.NextDue = DateTime.UtcNow.Add(retry);

        try
        {
            _db.Queues.UpdateQueueItem(_handle, _queueItem);
        }
        catch (Data.NotFoundException)
        {
            throw new QueueItemNotFoundException();
        }
    }

    public void Delete()
    {
        try
        {
            _db.Queues.DeleteQueueItem(_handle, _queueItem);
        }
        catch (Data.NotFoundException)
        {
            throw new QueueItemNotFoundException();
        }

        try
        {
            _db.Messages.DeleteMessage(_handle, _queueItem.Message);
        }
        catch (Data.NotFoundException)
        {
            throw new MessageNotFoundException();
        }
    }

    public void MoveToStore(zcfux.Mail.Store.Directory directory)
    {
        _db.Store.LinkMessage(_handle, _queueItem.Message, directory);

        try
        {
            _db.Queues.DeleteQueueItem(_handle, _queueItem);
        }
        catch (Data.NotFoundException)
        {
            throw new QueueItemNotFoundException();
        }
    }
}