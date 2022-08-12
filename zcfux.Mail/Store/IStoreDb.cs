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

namespace zcfux.Mail.Store;

public interface IStoreDb
{
    IDirectory NewDirectory(object handle, string name);

    IDirectory NewDirectory(object handle, IDirectory parent, string name);

    IDirectory GetDirectory(object handle, int id);

    IEnumerable<IDirectory> GetDirectories(object handle);

    IEnumerable<IDirectory> GetDirectories(object handle, IDirectory parent);

    void UpdateDirectory(object handle, IDirectory directory);

    void DeleteDirectory(object handle, IDirectory directory);

    IDirectoryEntry LinkMessage(object handle, IMessage message, IDirectory directory);

    void UnlinkMessage(object handle, IDirectoryEntry directoryEntry);

    IDirectoryEntry MoveMessage(object handle, IMessage message, IDirectory from, IDirectory to);

    IEnumerable<IDirectoryEntry> QueryMessages(object handle, Query query);
}