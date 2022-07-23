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
namespace zcfux.Mail;

public sealed class Attachment
{
    readonly IMailDb _db;
    readonly object _handle;
    readonly IAttachment _attachment;

    internal Attachment(IMailDb db, object handle, IAttachment attachment)
        => (_db, _handle, _attachment) = (db, handle, attachment);

    public long Id
        => _attachment.Id;

    public string Filename
        => _attachment.Filename;

    public Stream OpenRead()
        => _db.Attachments.OpenRead(_handle, Id);
}