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
namespace zcfux.Replication.Test.CouchDb;

static class Replication
{
    public static void Push(string from, string to)
    {
        var url = UrlBuilder.BuildServerUrl();

        using (var client = zcfux.Replication.CouchDb.Pool.ServerClients.TakeOrCreate(new Uri(url)))
        {
            var id = Guid.NewGuid().ToString("n");

            var response = client.Replicator.ReplicateAsync(id, $"{url}{from}", $"{url}{to}").Result;

            if (!response.IsSuccess)
            {
                throw new Exception(response.Reason);
            }

            WaitForReplicationJob(id);
        }
    }

    static void WaitForReplicationJob(string id)
    {
        var url = UrlBuilder.BuildServerUrl();

        using (var client = zcfux.Replication.CouchDb.Pool.Clients.TakeOrCreate(new Uri($"{url}_replicator")))
        {
            var deleted = false;

            while (!deleted)
            {
                var response = client.Documents.GetAsync(id).Result;

                if (!string.IsNullOrEmpty(response.Content)
                    && response.Content.Contains("\"completed\""))
                {
                    client.Documents.DeleteAsync(response.Id, response.Rev).Wait();

                    deleted = true;
                }
                else
                {
                    Thread.Sleep(250);
                }
            }
        }
    }
}