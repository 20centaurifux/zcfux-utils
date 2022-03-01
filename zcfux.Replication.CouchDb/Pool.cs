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
using zcfux.Pool;

namespace zcfux.Replication.CouchDb;

internal static class Pool
{
    static readonly Lazy<Pool<CouchClientResource>> CouchClientPool = new(() =>
    {
        var builder = new PoolBuilder<CouchClientResource>();

        return builder
            .WithFactory(uri => new CouchClientResource(uri))
            .Build();
    });

    public static Pool<CouchClientResource> Clients
        => CouchClientPool.Value;

    static readonly Lazy<Pool<ServerClientResource>> CouchServerClientPool = new(() =>
    {
        var builder = new PoolBuilder<ServerClientResource>();

        return builder
            .WithFactory(uri => new ServerClientResource(uri))
            .Build();
    });

    public static Pool<ServerClientResource> ServerClients
        => CouchServerClientPool.Value;
}