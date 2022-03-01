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
using zcfux.Replication.CouchDb;

namespace zcfux.Replication.Test.CouchDb;

public sealed class ReaderTests : AReaderTests
{
    protected override void CreateDb()
    {
        var url = UrlBuilder.BuildServerUrl();

        using (var client = zcfux.Replication.CouchDb.Pool.ServerClients.TakeOrCreate(new Uri(url)))
        {
            client.Databases.PutAsync("a").Wait();
        }
    }

    protected override void DropDb()
    {
        var url = UrlBuilder.BuildServerUrl();

        using (var client = zcfux.Replication.CouchDb.Pool.ServerClients.TakeOrCreate(new Uri(url)))
        {
            client.Databases.DeleteAsync("a").Wait();
        }
    }

    protected override AReader CreateReader(string side)
        => new Reader(side, UrlBuilder.BuildServerUrl());

    protected override AWriter CreateWriter(string side)
        => new Writer(side, UrlBuilder.BuildServerUrl());
}