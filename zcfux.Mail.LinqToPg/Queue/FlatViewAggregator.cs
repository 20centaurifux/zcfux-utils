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
using zcfux.Mail.Queue;

namespace zcfux.Mail.LinqToPg.Queue;

sealed class FlatViewAggregator
{
    readonly IQueryable<FlatQueuedMessageView> _records;

    IQueue? _queue;
    Message? _message;

    readonly IList<Address> _to = new List<Address>();
    readonly IList<Address> _cc = new List<Address>();
    readonly IList<Address> _bcc = new List<Address>();

    public FlatViewAggregator(IQueryable<FlatQueuedMessageView> messages)
        => _records = messages;

    public IEnumerable<IQueueItem> Aggregate()
    {
        foreach (var record in _records)
        {
            if (_queue?.Id != record.QueueId || _message?.Id != record.Id)
            {
                var queueItem = TryBuildEntry();

                if (queueItem != null)
                {
                    yield return queueItem;
                }

                _queue = new Queue
                {
                    Id = record.QueueId,
                    Name = record.Queue
                };

                _message = new Message
                {
                    Id = record.Id,
                    From = Address.FromString(record.From),
                    Subject = record.Subject,
                    TextBody = record.TextBody,
                    HtmlBody = record.HtmlBody
                };
            }

            if (record.To != null)
            {
                _to.Add(Address.FromString(record.To));
            }

            if (record.Cc != null)
            {
                _cc.Add(Address.FromString(record.Cc));
            }

            if (record.Bcc != null)
            {
                _bcc.Add(Address.FromString(record.Bcc));
            }
        }

        var tail = TryBuildEntry();

        if (tail != null)
        {
            yield return tail;
        }
    }

    IQueueItem? TryBuildEntry()
    {
        IQueueItem? queueItem = null;

        if (_queue != null && _message != null)
        {
            _message.To = _to.ToArray();
            _message.Cc = _cc.ToArray();
            _message.Bcc = _bcc.ToArray();

            queueItem = new QueueItem
            {
                Queue = _queue,
                Message = _message
            };

            _queue = null;
            _message = null;

            _to.Clear();
            _cc.Clear();
            _bcc.Clear();
        }

        return queueItem;
    }
}