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
using LinqToDB.Mapping;
using zcfux.Mail.Queue;

namespace zcfux.Mail.LinqToPg.Queue;

[Table(Schema = "mail", Name = "QueueItem")]
sealed class QueueItemRelation : IQueueItem
{
#pragma warning disable CS8618
    public QueueItemRelation()
    {
    }

    public QueueItemRelation(IQueueItem queueItem)
    {
        QueueId = queueItem.Queue.Id;
        Queue = new QueueRelation(queueItem.Queue);
        MessageId = queueItem.Message.Id;
        Message = new MessageRelation(queueItem.Message);
        Created = queueItem.Created;
        EndOfLife = queueItem.EndOfLife;
        NextDue = queueItem.NextDue;
    }

    public QueueItemRelation(IQueue queue, IMessage message)
    {
        QueueId = queue.Id;
        Queue = new QueueRelation(queue);
        MessageId = message.Id;
        Message = new MessageRelation(message);
    }

    [Column(Name = "QueueId", IsPrimaryKey = true)]
    public int QueueId { get; set; }

    [Association(ThisKey = "QueueId", OtherKey = "Id")]
    public QueueRelation Queue { get; set; }

    IQueue IQueueItem.Queue => Queue;

    [Column(Name = "MessageId", IsPrimaryKey = true)]
    public long MessageId { get; set; }

    [Association(ThisKey = "MessageId", OtherKey = "Id")]
    public MessageRelation Message { get; set; }

    IMessage IQueueItem.Message => Message;

    [Column(Name = "Created")]
    public DateTime Created { get; set; }

    DateTime IQueueItem.Created => Created;

    [Column(Name = "EndOfLife")]
    public DateTime EndOfLife { get; set; }

    DateTime IQueueItem.EndOfLife => EndOfLife;

    [Column(Name = "NextDue")]
    public DateTime? NextDue { get; set; }

    DateTime? IQueueItem.NextDue => NextDue;

    [Column(Name = "Errors")]
    public int Errors { get; set; }

    int IQueueItem.Errors => Errors;
#pragma warning restore CS8618
}