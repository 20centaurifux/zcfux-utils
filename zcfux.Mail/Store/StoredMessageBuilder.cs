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
namespace zcfux.Mail.Store;

public sealed class StoredMessageBuilder
{
    readonly IDb _db;
    readonly object _handle;

    MessageBuilder _messageBuilder;
    IDirectory? _directory;

    internal StoredMessageBuilder(IDb db, object handle)
        => (_db, _handle, _messageBuilder) = (db, handle, new MessageBuilder(db, handle));

    StoredMessageBuilder Clone()
    {
        var builder = new StoredMessageBuilder(_db, _handle)
        {
            _messageBuilder = _messageBuilder,
            _directory = _directory
        };

        return builder;
    }

    internal StoredMessageBuilder WithDirectory(IDirectory directory)
    {
        var builder = Clone();

        builder._directory = directory;

        return builder;
    }

    public StoredMessageBuilder WithFrom(Address address)
    {
        var builder = Clone();

        builder._messageBuilder.WithFrom(address);

        return builder;
    }

    public StoredMessageBuilder WithFrom(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithFrom(displayName, address);

        return builder;
    }

    public StoredMessageBuilder WithTo(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTo(address);

        return builder;
    }

    public StoredMessageBuilder WithTo(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTo(displayName, address);

        return builder;
    }

    public StoredMessageBuilder WithCc(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithCc(address);

        return builder;
    }

    public StoredMessageBuilder WithCc(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithCc(displayName, address);

        return builder;
    }

    public StoredMessageBuilder WithBcc(Address address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithBcc(address);

        return builder;
    }

    public StoredMessageBuilder WithBcc(string displayName, string address)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithBcc(displayName, address);

        return builder;
    }

    public StoredMessageBuilder WithSubject(string subject)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithSubject(subject);

        return builder;
    }

    public StoredMessageBuilder WithTextBody(string textBody)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithTextBody(textBody);

        return builder;
    }

    public StoredMessageBuilder WithHtmlBody(string htmlBody)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithHtmlBody(htmlBody);

        return builder;
    }

    public StoredMessageBuilder WithAttachment(string absoluteFilename)
    {
        var builder = Clone();

        builder._messageBuilder = builder._messageBuilder.WithAttachment(absoluteFilename);

        return builder;
    }

    public StoredMessage Store()
    {
        if (_directory == null)
        {
            throw new BuilderException("Directory is undefined.");
        }

        try
        {
            var directory = _db.Store.GetDirectory(_handle, _directory.Id);

            var message = _messageBuilder.Store();

            var directoryEntry = _db.Store.LinkMessage(_handle, message, directory);

            return new StoredMessage(_db, _handle, directoryEntry);
        }
        catch(Data.NotFoundException)
        {
            throw new DirectoryNotFoundException();
        }
    }
}