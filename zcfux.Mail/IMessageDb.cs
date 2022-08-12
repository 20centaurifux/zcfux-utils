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

namespace zcfux.Mail;
public interface IMessageDb
{
    IMessage NewMessage(
        object handle,
        Address from,
        IEnumerable<Address> to,
        IEnumerable<Address> cc,
        IEnumerable<Address> bcc,
        string subject,
        string? textBody,
        string? htmlBody);

    IMessage GetMessage(
        object handle,
        long id);
    
    void DeleteMessage(
        object handle,
        IMessage message);

    IAttachment NewAttachment(
        object handle,
        IMessage message,
        string filename,
        Stream stream);

    IEnumerable<IAttachment> GetAttachments(
        object handle,
        IMessage message);

    Stream ReadAttachment(
        object handle,
        long id);

    void DeleteAttachment(
        object handle,
        IAttachment attachment);
}