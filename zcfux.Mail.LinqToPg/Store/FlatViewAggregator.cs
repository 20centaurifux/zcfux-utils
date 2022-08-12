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
using zcfux.Mail.Store;

namespace zcfux.Mail.LinqToPg.Store;

internal sealed class FlatViewAggregator
{
    readonly IQueryable<FlatStoredMessageView> _records;

    IDirectory? _directory;
    Message? _message;

    readonly IList<Address> _to = new List<Address>();
    readonly IList<Address> _cc = new List<Address>();
    readonly IList<Address> _bcc = new List<Address>();

    public FlatViewAggregator(IQueryable<FlatStoredMessageView> messages)
        => _records = messages;

    public IEnumerable<IDirectoryEntry> Aggregate()
    {
        foreach (var record in _records)
        {
            if (_directory?.Id != record.DirectoryId || _message?.Id != record.Id)
            {
                var directoryEntry = TryBuildEntry();

                if (directoryEntry != null)
                {
                    yield return directoryEntry;
                }

                StartNewDirectoryEntry(record);
            }

            AddReceivers(record);
        }

        var tail = TryBuildEntry();

        if (tail != null)
        {
            yield return tail;
        }
    }

    IDirectoryEntry? TryBuildEntry()
    {
        DirectoryEntry? directoryEntry = null;

        if (_directory != null && _message != null)
        {
            _message.To = _to.ToArray();
            _message.Cc = _cc.ToArray();
            _message.Bcc = _bcc.ToArray();

            directoryEntry = new DirectoryEntry
            {
                Directory = _directory,
                Message = _message
            };

            _directory = null;
            _message = null;

            _to.Clear();
            _cc.Clear();
            _bcc.Clear();
        }

        return directoryEntry;
    }

    void StartNewDirectoryEntry(FlatStoredMessageView flatStoredMessage)
    {
        _directory = new Directory
        {
            Id = flatStoredMessage.DirectoryId,
            Name = flatStoredMessage.Directory,
            ParentId = flatStoredMessage.ParentId
        };

        _message = new Message
        {
            Id = flatStoredMessage.Id,
            From = Address.FromString(flatStoredMessage.From),
            Subject = flatStoredMessage.Subject,
            TextBody = flatStoredMessage.TextBody,
            HtmlBody = flatStoredMessage.HtmlBody
        };
    }

    void AddReceivers(FlatStoredMessageView flatStoreMessage)
    {
        if (flatStoreMessage.To != null)
        {
            _to.Add(Address.FromString(flatStoreMessage.To));
        }

        if (flatStoreMessage.Cc != null)
        {
            _cc.Add(Address.FromString(flatStoreMessage.Cc));
        }

        if (flatStoreMessage.Bcc != null)
        {
            _bcc.Add(Address.FromString(flatStoreMessage.Bcc));
        }
    }
}