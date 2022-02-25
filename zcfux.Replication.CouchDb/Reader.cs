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
using zcfux.Replication.Generic;

namespace zcfux.Replication.CouchDb;

public sealed class Reader : AReader
{
    readonly string _url;

    public Reader(string side, string url)
        : base(side)
        => _url = url;

    public override Version<T> Read<T>(Guid guid)
    {
        using (var client = NewClient())
        {
            var id = guid.ToString("n");

            var response = client.Documents.GetAsync(id).Result;

            if (!response.IsSuccess)
            {
                throw new Exception(response.Reason);
            }

            var doc = JsonConvert.DeserializeObject<Document<T>>(response.Content);

            return new Version<T>(doc!.Entity, response.Rev, doc.Side, doc.Modified);
        }
    }

    IMyCouchClient NewClient()
        => new MyCouchClient(_url, Side);
}