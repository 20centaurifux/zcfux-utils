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
using zcfux.Mail.Store;

namespace zcfux.Mail.LinqToPg.Store;

[Table(Schema = "mail", Name = "DirectoryEntry")]
sealed class DirectoryEntryRelation : IDirectoryEntry
{
#pragma warning disable CS8618
    public DirectoryEntryRelation()
    {
    }

    public DirectoryEntryRelation(IDirectoryEntry directoryEntry)
    {
        MessageId = directoryEntry.Message.Id;
        Message = new MessageRelation(directoryEntry.Message);

        DirectoryId = directoryEntry.Directory.Id;
        Directory = new DirectoryRelation(directoryEntry.Directory);
    }

    public DirectoryEntryRelation(IMessage message, IDirectory directory)
    {
        MessageId = message.Id;
        Message = new MessageRelation(message);

        DirectoryId = directory.Id;
        Directory = new DirectoryRelation(directory);
    }

    [Column(Name = "DirectoryId", IsPrimaryKey = true)]
    public int DirectoryId { get; set; }

    [Association(ThisKey = "DirectoryId", OtherKey = "Id")]
    public DirectoryRelation Directory { get; set; }

    IDirectory IDirectoryEntry.Directory => Directory;

    [Column(Name = "MessageId", IsPrimaryKey = true)]
    public long MessageId { get; set; }

    [Association(ThisKey = "MessageId", OtherKey = "Id")]
    public MessageRelation Message { get; set; }

    IMessage IDirectoryEntry.Message => Message;
#pragma warning restore CS8618
}