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
namespace zcfux.Mail.Queue;

public sealed class QueuedMessageBuilder
{
    readonly IDb _db;
    readonly object _handle;

    MessageBuilder _messageBuilder;
    IQueue? _queue;
    TimeSpan _timeToLive = TimeSpan.FromHours(1);
    DateTime? _nextDue;

    internal QueuedMessageBuilder(IDb db, object handle)
        => (_db, _handle, _messageBuilder) = (db, handle, new MessageBuilder(db, handle));

    QueuedMessageBuilder Clone()
    {
        var builder = new QueuedMessageBuilder(_db, _handle)
        {
            _messageBuilder = _messageBuilder,
            _queue = _queue,
            _timeToLive = _timeToLive,
            _nextDue = _nextDue
        };

        return builder;
    }

    internal QueuedMessageBuilder WithQueue(IQueue queue)
    {
        var builder = Clone();

        builder._queue = queue;

        return builder;
    }

    public QueuedMessageBuilder WithTimeToLive(TimeSpan timeToLive)
    {
        var builder = Clone();

        builder._timeToLive = timeToLive;

        return builder;
    }

    public QueuedMessageBuilder WithNextDue(DateTime nextDue)
    {
        var builder = Clone();

        builder._nextDue = nextDue;

        return builder;
    }

    public QueuedMessageBuilder WithFrom(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithFrom(address);

        return builder;
    }

    public QueuedMessageBuilder WithFrom(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithFrom(displayName, address);

        return builder;
    }

    public QueuedMessageBuilder WithTo(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTo(address);

        return builder;
    }

    public QueuedMessageBuilder WithTo(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTo(displayName, address);

        return builder;
    }

    public QueuedMessageBuilder WithCc(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithCc(address);

        return builder;
    }

    public QueuedMessageBuilder WithCc(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithCc(displayName, address);

        return builder;
    }

    public QueuedMessageBuilder WithBcc(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithBcc(address);

        return builder;
    }

    public QueuedMessageBuilder WithBcc(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithBcc(displayName, address);

        return builder;
    }

    public QueuedMessageBuilder WithSubject(string subject)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithSubject(subject);

        return builder;
    }

    public QueuedMessageBuilder WithTextBody(string textBody)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTextBody(textBody);

        return builder;
    }

    public QueuedMessageBuilder WithHtmlBody(string htmlBody)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithHtmlBody(htmlBody);

        return builder;
    }

    public QueuedMessageBuilder WithAttachment(string absoluteFilename)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithAttachment(absoluteFilename);

        return builder;
    }

    public QueuedMessage Store()
    {
        if (_queue == null)
        {
            throw new BuilderException("Queue is undefined.");
        }

        try
        {
            var queue = _db.Queues.GetQueue(_handle, _queue.Id);

            var message = _messageBuilder.Store();

            var queuedItem = _db.Queues.NewQueueItem(
                _handle,
                queue,
                message,
                _timeToLive,
                _nextDue ?? DateTime.UtcNow.Add(TimeSpan.FromSeconds(15)));

            return new QueuedMessage(_db, _handle, queuedItem);
        }
        catch(Data.NotFoundException)
        {
            throw new QueueNotFoundException();
        }
    }
}