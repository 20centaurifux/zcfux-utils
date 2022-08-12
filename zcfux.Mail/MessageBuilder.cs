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
using System.Collections.Immutable;

namespace zcfux.Mail;

internal sealed class MessageBuilder
{
    readonly IDb _db;
    readonly object _handle;

    Address? _from;
    ImmutableHashSet<Address> _to = ImmutableHashSet.Create<Address>();
    ImmutableHashSet<Address> _cc = ImmutableHashSet.Create<Address>();
    ImmutableHashSet<Address> _bcc = ImmutableHashSet.Create<Address>();
    string? _subject;
    string? _textBody;
    string? _htmlBody;
    ImmutableHashSet<string> _attachments = ImmutableHashSet.Create<string>();

    public MessageBuilder(IDb db, object handle)
        => (_db, _handle) = (db, handle);

    MessageBuilder Clone()
    {
        var builder = new MessageBuilder(_db, _handle)
        {
            _from = _from,
            _to = _to,
            _cc = _cc,
            _bcc = _bcc,
            _subject = _subject,
            _textBody = _textBody,
            _htmlBody = _htmlBody
        };

        return builder;
    }

    public MessageBuilder WithFrom(Address address)
    {
        var builder = Clone();

        builder._from = address;

        return builder;
    }

    public MessageBuilder WithFrom(string displayName, string address)
        => WithFrom(new Address(displayName, address));

    public MessageBuilder WithTo(Address address)
    {
        var builder = Clone();

        builder._to = _to.Add(address);

        return builder;
    }

    public MessageBuilder WithTo(string displayName, string address)
        => WithTo(new Address(displayName, address));

    public MessageBuilder WithCc(Address address)
    {
        var builder = Clone();

        builder._cc = _cc.Add(address);

        return builder;
    }

    public MessageBuilder WithCc(string displayName, string address)
        => WithCc(new Address(displayName, address));

    public MessageBuilder WithBcc(Address address)
    {
        var builder = Clone();

        builder._bcc = _bcc.Add(address);

        return builder;
    }

    public MessageBuilder WithBcc(string displayName, string address)
        => WithBcc(new Address(displayName, address));

    public MessageBuilder WithSubject(string subject)
    {
        var builder = Clone();

        builder._subject = subject;

        return builder;
    }

    public MessageBuilder WithTextBody(string textBody)
    {
        var builder = Clone();

        builder._textBody = textBody;

        return builder;
    }

    public MessageBuilder WithHtmlBody(string htmlBody)
    {
        var builder = Clone();

        builder._htmlBody = htmlBody;

        return builder;
    }

    public MessageBuilder WithAttachment(string absoluteFilename)
    {
        var builder = Clone();

        builder._attachments = _attachments.Add(absoluteFilename);

        return builder;
    }

    public IMessage Store()
    {
        ThrowIfIncomplete();

        var message = _db.Messages.NewMessage(
            _handle,
            _from!,
            _to,
            _cc,
            _bcc,
            _subject!,
            _textBody,
            _htmlBody);

        StoreAttachments(message);

        return message;
    }

    void StoreAttachments(IMessage message)
    {
        foreach (var absoluteFilename in _attachments)
        {
            var filename = Path.GetFileName(absoluteFilename);

            using (var stream = File.OpenRead(absoluteFilename))
            {
                _db.Messages.NewAttachment(_handle, message, filename, stream);
            }
        }
    }

    void ThrowIfIncomplete()
    {
        if (_from == null)
        {
            throw new BuilderException("Sender is undefined.");
        }

        if (!_to.Any())
        {
            throw new BuilderException("Receiver is undefined.");
        }

        if (string.IsNullOrEmpty(_subject))
        {
            throw new BuilderException("Subject is undefined.");
        }

        if (string.IsNullOrEmpty(_textBody) && string.IsNullOrEmpty(_htmlBody))
        {
            throw new BuilderException("Body is undefined.");
        }
    }
}