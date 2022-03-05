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
using MyCouch.Serialization;
using zcfux.Pool;

namespace zcfux.Replication.CouchDb;

public sealed class CouchClientResource : AResource, IMyCouchClient
{
    readonly MyCouchClient _client;

    public CouchClientResource(Uri uri)
        : base(uri)
    {
        var builder = new UriBuilder(uri.AbsoluteUri)
        {
            Path = null
        };

        var dbName = uri.LocalPath.Substring(1);

        _client = new MyCouchClient(builder.Uri, dbName, new ClientBootstrapper());
    }

    public override void Suspend()
    {
        base.Suspend();

        if (_client.Connection is ISuspend suspend)
        {
            suspend.Suspend();
        }
    }

    public override void Free()
        => _client.Dispose();

    public IDbConnection Connection
        => _client.Connection;

    public ISerializer Serializer
        => _client.Serializer;

    public ISerializer DocumentSerializer
        => _client.DocumentSerializer;

    public IChanges Changes
        => _client.Changes;

    public IAttachments Attachments
        => _client.Attachments;

    public IDatabase Database
        => _client.Database;

    public IDocuments Documents
        => _client.Documents;

    public IEntities Entities
        => _client.Entities;

    public IQueries Queries
        => _client.Queries;

    public ISearches Searches
        => _client.Searches;

    public IViews Views
        => _client.Views;
}