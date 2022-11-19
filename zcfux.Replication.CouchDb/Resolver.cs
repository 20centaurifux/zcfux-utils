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
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using zcfux.Replication.Generic;

namespace zcfux.Replication.CouchDb;

public sealed class Resolver : AResolver
{
    readonly string _url;

    public Resolver(string side, string url)
        : base(side)
        => _url = url;

    public override IVersion Resolve(IVersion version)
    {
        using (var client = NewClient())
        {
            IVersion? mergeResult = null;

            var id = version.Entity.Guid.ToString("n");

            while (mergeResult == null)
            {
                mergeResult = Resolve(client, id);
            }

            return mergeResult;
        }
    }

    IVersion Resolve(IMyCouchClient client, string id)
    {
        IVersion? mergedVersion = null;

        var response = ReceiveDocumentWithConflicts(client, id);

        var version = BuildVersion(response);

        var conflicts = ReceiveConflicts(client, id, response.Conflicts).ToArray();

        if (conflicts.Any())
        {
            mergedVersion = Algorithms.Merge(version, conflicts);

            var revisions = new List<IVersion>
            {
                version
            };

            revisions.AddRange(conflicts);

            var success = SelectWinner(client, id, mergedVersion, revisions.ToArray());

            if (!success)
            {
                mergedVersion = null;
            }
        }
        else
        {
            mergedVersion = version;
        }

        return mergedVersion!;
    }

    IEnumerable<IVersion> ReceiveConflicts(IMyCouchClient client, string id, string[]? revs)
    {
        if (revs != null)
        {
            foreach (var rev in revs)
            {
                var response = ReceiveRevision(client, id, rev);

                var version = BuildVersion(response);

                yield return version;
            }
        }
    }

    DocumentResponse ReceiveDocumentWithConflicts(IMyCouchClient client, string id)
    {
        var req = new GetDocumentRequest(id)
        {
            Conflicts = true
        };

        var response = client.Documents.GetAsync(req).Result;

        if (!response.IsSuccess)
        {
            throw new Exception(response.Reason);
        }

        return response;
    }

    DocumentResponse ReceiveRevision(IMyCouchClient client, string id, string revision)
    {
        var response = client.Documents.GetAsync(id, revision).Result;

        if (!response.IsSuccess)
        {
            throw new Exception(response.Reason);
        }

        return response;
    }

    static IVersion BuildVersion(DocumentResponse response)
    {
        var doc = JsonConvert.DeserializeObject<Document<JObject>>(response.Content);

        var type = TypeMap.Get(doc!.Kind);

        var entity = doc.Entity.ToObject(type) as IEntity;

        var version = new Version<IEntity>(entity!, response.Rev, doc.Side, doc.Modified, doc.Deleted);

        return version;
    }

    bool SelectWinner(IMyCouchClient client, string id, IVersion winner, IVersion[] revisions)
    {
        var req = new BulkRequest();

        foreach (var revision in revisions)
        {
            if (revision.Revision != winner.Revision)
            {
                req.Delete(id, revision.Revision);
            }
        }

        if (winner.IsNew)
        {
            var doc = new Document<IEntity>
            {
                _id = winner.Entity.Guid.ToString("n"),
                Kind = winner.GetType().FullName!,
                Entity = winner.Entity,
                Modified = winner.Modified,
                Side = winner.Side
            };

            req.Include(JsonConvert.SerializeObject(winner));
        }

        var response = client.Documents.BulkAsync(req).Result;

        var success = response.IsSuccess;

        if (!success && response.StatusCode != HttpStatusCode.Conflict)
        {
            throw new Exception(response.Reason);
        }

        return success;
    }

    IMyCouchClient NewClient()
        => new MyCouchClient(_url, Side);
}