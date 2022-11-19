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
using System.Net;
using MyCouch;
using Newtonsoft.Json;
using zcfux.Replication.Generic;

namespace zcfux.Replication.CouchDb;

internal sealed class Updater<T>
    where T : IEntity
{
    readonly string _url;
    readonly string _side;

    public Updater(string url, string side)
        => (_url, _side) = (url, side);

    public IVersion<T> Apply(T update,
        string revision,
        string side,
        DateTime timestamp,
        Func<Version<T>, Version<T>[], Version<T>> merge)
    {
        using (var client = Pool.Clients.TakeOrCreate(new Uri($"{_url}{_side}")))
        {
            IVersion<T> result;

            var latestVersion = LatestVersion(client, update.Guid);

            if (latestVersion.Entity.Equals(update))
            {
                result = latestVersion;
            }
            else
            {
                var winner = new Version<T>(update, null, side, timestamp, false);

                if (revision != latestVersion.Revision)
                {
                    winner = merge(latestVersion, new[] { winner });
                }

                if (winner.IsNew)
                {
                    var doc = Converter.ToDocument(winner);

                    var json = JsonConvert.SerializeObject(doc);

                    var response = client.Documents.PutAsync(doc._id, latestVersion.Revision, json).Result;

                    if (response.IsSuccess)
                    {
                        result = new Version<T>(winner.Entity, response.Rev, winner.Side, winner.Modified, winner.IsDeleted);
                    }
                    else if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        result = Apply(update, revision, side, timestamp, merge);
                    }
                    else
                    {
                        throw new Exception(response.Reason);
                    }
                }
                else
                {
                    result = winner;
                }
            }

            return result;
        }
    }

    Version<T> LatestVersion(IMyCouchClient client, Guid guid)
    {
        var id = guid.ToString("n");

        var response = client.Documents.GetAsync(id).Result;

        if (!response.IsSuccess)
        {
            throw new Exception(response.Reason);
        }

        var doc = JsonConvert.DeserializeObject<Document<T>>(response.Content);

        return new Version<T>(doc!.Entity, response.Rev, doc.Side, doc.Modified, doc.Deleted);
    }
}