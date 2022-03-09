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
using MyCouch;
using Newtonsoft.Json;
using System.Net;
using zcfux.Replication.Generic;

namespace zcfux.Replication.CouchDb;

public sealed class Writer : AWriter
{
    readonly string _url;

    public Writer(string side, string url)
        : base(side)
        => _url = url;

    public override ECreateResult TryCreate<T>(T entity, DateTime timestamp, out IVersion<T>? version)
    {
        version = null;

        using (var client = Pool.Clients.TakeOrCreate(new Uri($"{_url}{Side}")))
        {
            var id = entity.Guid.ToString("n");

            var doc = Converter.ToDocument(entity);

            doc.Side = Side;
            doc.Modified = timestamp;

            var json = JsonConvert.SerializeObject(doc);

            var result = ECreateResult.Success;

            var putResponse = client.Documents.PutAsync(doc._id, json).Result;

            if (putResponse.IsSuccess)
            {
                version = new Version<T>(entity, putResponse.Rev, Side, timestamp, false);
            }
            else if (putResponse.StatusCode == HttpStatusCode.Conflict)
            {
                var getResponse = client.Documents.GetAsync(id).Result;

                if (getResponse.IsSuccess)
                {
                    doc = JsonConvert.DeserializeObject<Document<T>>(getResponse.Content);

                    version = new Version<T>(doc!.Entity, getResponse.Rev, doc.Side, doc.Modified, doc.Deleted);

                    result = ECreateResult.Conflict;
                }
                else
                {
                    throw new Exception(getResponse.Reason);
                }
            }
            else
            {
                throw new Exception(putResponse.Reason);
            }

            return result;
        }
    }

    public override IVersion<T> Update<T>(T entity, string revision, DateTime timestamp, IMergeAlgorithm mergeAlgorithm)
    {
        var updater = new Updater<T>(_url, Side);

        return updater.Apply(entity, revision, Side, timestamp, (latestVersion, conflicts) =>
        {
            var conflictVersions = conflicts.Cast<IVersion>().ToArray();

            var mergedVersion = mergeAlgorithm.Merge(latestVersion, conflictVersions);

            return new Version<T>(mergedVersion);
        });
    }

    public override IVersion<T> Update<T>(T entity, string revision, DateTime timestamp, IMergeAlgorithm<T> mergeAlgorithm)
    {
        var updater = new Updater<T>(_url, Side);

        return updater.Apply(entity, revision, Side, timestamp, (latestVersion, conflicts) =>
        {
            var conflictVersions = conflicts.Cast<IVersion<T>>().ToArray();

            var mergedVersion = mergeAlgorithm.Merge(latestVersion, conflictVersions);

            return new Version<T>(mergedVersion);
        });
    }

    public override IVersion<T> Delete<T>(Guid guid, DateTime timestamp)
    {
        using (var client = Pool.Clients.TakeOrCreate(new Uri($"{_url}{Side}")))
        {
            var id = guid.ToString("n");

            var (doc, rev) = GetDocument<T>(client, id);

            while (!doc.Deleted)
            {
                doc.Side = Side;
                doc.Modified = timestamp;
                doc.Deleted = true;

                var json = JsonConvert.SerializeObject(doc);

                var response = client.Documents.PutAsync(id, rev, json).Result;

                if (response.IsSuccess)
                {
                    rev = response.Rev;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    (doc, rev) = GetDocument<T>(client, id);
                }
                else
                {
                    throw new Exception(response.Reason);
                }
            }

            return new Version<T>(doc.Entity, rev, doc.Side, doc.Modified, doc.Deleted);
        }
    }

    static (Document<T>, string) GetDocument<T>(IMyCouchClient client, string id)
        where T : IEntity
    {
            var response = client.Documents.GetAsync(id).Result;

            if (!response.IsSuccess)
            {
                throw new Exception(response.Reason);
            }

            var doc = JsonConvert.DeserializeObject<Document<T>>(response.Content);

            return (doc!, response.Rev);
    }
}