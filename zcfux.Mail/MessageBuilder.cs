﻿/***************************************************************************
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

public sealed class MessageBuilder
{
    readonly IMailDb _db;
    readonly object _handle;
    IDirectory? _directory;
    Address? _from;
    ImmutableHashSet<Address> _to = ImmutableHashSet.Create<Address>();
    ImmutableHashSet<Address> _cc = ImmutableHashSet.Create<Address>();
    ImmutableHashSet<Address> _bcc = ImmutableHashSet.Create<Address>();
    string? _subject;
    string? _textBody;
    string? _htmlBody;
    ImmutableHashSet<string> _attachments = ImmutableHashSet.Create<string>();

    public MessageBuilder(IMailDb db, object handle)
        => (_db, _handle) = (db, handle);

    MessageBuilder Clone()
    {
        var builder = new MessageBuilder(_db, _handle)
        {
            _directory = _directory,
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

    public MessageBuilder WithDirectory(IDirectory directory)
    {
        var builder = Clone();

        builder._directory = directory;

        return builder;
    }

    public MessageBuilder WithFrom(Address address)
    {
        var builder = Clone();

        builder._from = address;

        return builder;
    }

    public MessageBuilder WithFrom(string displayName, string mailAddress)
        => WithFrom(new Address(displayName, mailAddress));

    public MessageBuilder WithTo(Address address)
    {
        var builder = Clone();

        builder._to = _to.Add(address);

        return builder;
    }

    public MessageBuilder WithTo(string displayName, string mailAddress)
        => WithTo(new Address(displayName, mailAddress));

    public MessageBuilder WithCc(Address address)
    {
        var builder = Clone();

        builder._cc = _cc.Add(address);

        return builder;
    }

    public MessageBuilder WithCc(string displayName, string mailAddress)
        => WithCc(new Address(displayName, mailAddress));

    public MessageBuilder WithBcc(Address address)
    {
        var builder = Clone();

        builder._bcc = _bcc.Add(address);

        return builder;
    }

    public MessageBuilder WithBcc(string displayName, string mailAddress)
        => WithBcc(new Address(displayName, mailAddress));

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

    public Message Store()
    {
        if (_directory == null)
        {
            throw new MessageBuilderException("Directory is undefined.");
        }

        ThrowIfIncomplete();

        var message = _db.Messages.NewMessage(
            _handle,
            _directory!,
            _from!,
            _to,
            _cc,
            _bcc,
            _subject!,
            _textBody,
            _htmlBody);

        StoreAttachments(message);

        return new Message(_db, _handle, message);
    }

    void StoreAttachments(IMessage message)
    {
        foreach (var absoluteFilename in _attachments)
        {
            var filename = Path.GetFileName(absoluteFilename);

            using (var stream = File.OpenRead(absoluteFilename))
            {
                _db.Attachments.NewAttachment(_handle, message, filename, stream);
            }
        }
    }

    void ThrowIfIncomplete()
    {
        if (_from == null)
        {
            throw new MessageBuilderException("Sender is undefined.");
        }

        if (!_to.Any())
        {
            throw new MessageBuilderException("Receiver is undefined.");
        }

        if (string.IsNullOrEmpty(_subject))
        {
            throw new MessageBuilderException("Subject is undefined.");
        }

        if (string.IsNullOrEmpty(_textBody) && string.IsNullOrEmpty(_htmlBody))
        {
            throw new MessageBuilderException("Body is undefined.");
        }
    }
}