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
using zcfux.Mail.LinqToPg.Queue;
using zcfux.Mail.LinqToPg.Store;
using zcfux.Mail.Queue;
using zcfux.Mail.Store;

namespace zcfux.Mail.LinqToPg;

public sealed class Db : IDb
{
    static readonly Lazy<IMessageDb> LazyMessageDb
        = new(Data.Proxy.Factory.ConvertHandle<IMessageDb, MessageDb>);

    static readonly Lazy<IStoreDb> LazyStoreDb
        = new(Data.Proxy.Factory.ConvertHandle<IStoreDb, StoreDb>);

    static readonly Lazy<IQueueDb> LazyQueueDb
        = new(Data.Proxy.Factory.ConvertHandle<IQueueDb, QueueDb>);

    public IMessageDb Messages => LazyMessageDb.Value;

    public IStoreDb Store => LazyStoreDb.Value;

    public IQueueDb Queues => LazyQueueDb.Value;
}