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

    public override CreateResult<T> TryCreate<T>(T entity, DateTime timestamp)
    {
        using (var client = NewClient())
        {
            var id = entity.Guid.ToString("n");

            var doc = Converter.ToDocument(entity);

            doc.Side = Side;
            doc.Modified = timestamp;

            var json = JsonConvert.SerializeObject(doc);

            var putResponse = client.Documents.PutAsync(doc._id, json).Result;

            CreateResult<T>? result = null;

            if (putResponse.IsSuccess)
            {
                var version = new Version<T>(entity, putResponse.Rev, Side, timestamp);

                result = CreateResult<T>.Created(version);
            }
            else if (putResponse.StatusCode == HttpStatusCode.Conflict)
            {
                var getResponse = client.Documents.GetAsync(id).Result;

                if (getResponse.IsSuccess)
                {
                    doc = JsonConvert.DeserializeObject<Document<T>>(getResponse.Content);

                    var version = new Version<T>(doc!.Entity, getResponse.Rev, doc.Side, doc.Modified);

                    result = CreateResult<T>.Conflict(version);
                }
            }

            return result ?? throw new Exception(putResponse.Reason);
        }
    }

    public override Version<T> Update<T>(T entity, string revision, DateTime timestamp, IMerge<T> merge)
    {
        var updater = new Updater<T>(_url, Side);

        return updater.Apply(entity, revision, Side, timestamp, merge);
    }

    IMyCouchClient NewClient()
        => new MyCouchClient(_url, Side);
}