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
using LinqToDB;
using LinqToDB.Data;
using zcfux.Data.LinqToDB;

namespace zcfux.Audit.LinqToPg;

internal sealed class Events
{
    record Pair(long AssocId, long TopicId);

    public void InsertEventKind(Handle handle, IEventKind kind)
        => handle.Db().Insert(new EventKindRelation(kind));

    public IEventKind GetEventKind(Handle handle, int id)
        => handle.Db()
            .GetTable<EventKindRelation>()
            .Single(kind => kind.Id == id);

    public IEvent NewEvent(Handle handle, IEventKind kind, ESeverity severity, DateTime createdAt, ITopic? topic)
    {
        var ev = new EventRelation(kind, severity, createdAt, topic);

        ev.Id = Convert.ToInt64(handle.Db().InsertWithIdentity(ev));

        return ev;
    }

    public ICatalogue CreateCatalogue(Handle handle, ECatalogue catalogue)
        => (catalogue == ECatalogue.Recent)
            ? new RecentCatalogue(handle)
            : new ArchiveCatalogue(handle);

    public void ArchiveEvents(Handle handle, DateTime before)
    {
        var db = handle.Db();

        var outdated = OutdatedAssociations(db, before);

        db.GetTable<TopicAssociationRelation>()
            .Where(a => outdated.Any(id => id == a.Id))
            .Set(a => a.Archived, true)
            .Update();

        db.GetTable<EventRelation>()
            .Where(ev => ev.CreatedAt < before)
            .Set(ev => ev.Archived, true)
            .Update();

        using (var cmd = db.Connection.CreateCommand())
        {
            cmd.Transaction = db.Transaction;

            cmd.CommandText = @"REFRESH MATERIALIZED VIEW CONCURRENTLY audit.""ArchivedEventView""";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"REFRESH MATERIALIZED VIEW CONCURRENTLY audit.""ArchivedEdgeView""";

            cmd.ExecuteNonQuery();
        }
    }

    static IQueryable<long> OutdatedAssociations(DataConnection db, DateTime before)
    {
        var cte = db.GetCte<Pair>(outdated =>
        {
            return (
                    from e in db.GetTable<EventRelation>()
                        .TableName("RecentEvent")
                        .Where(ev => ev.CreatedAt < before && ev.TopicId.HasValue)
                    from a in db.GetTable<TopicAssociationRelation>()
                        .TableName("RecentTopicAssociation")
                        .InnerJoin(m => m.Topic1 == e.TopicId!.Value)
                    select new Pair(a.Id, a.Topic1)
                )
                .Concat(
                    from a in db.GetTable<TopicAssociationRelation>()
                        .TableName("RecentTopicAssociation")
                    from p in outdated.InnerJoin(t => t.TopicId == a.Topic1)
                    select new Pair(a.Id, a.Topic2)
                );
        });

        return (from t in cte select t.AssocId);
    }
}