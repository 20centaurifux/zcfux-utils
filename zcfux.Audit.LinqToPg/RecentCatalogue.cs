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
using zcfux.Filter;
using zcfux.Filter.Linq;
using zcfux.Translation.LinqToDB;

namespace zcfux.Audit.LinqToPg;

sealed class RecentCatalogue : ICatalogue
{
    readonly Handle _handle;
    readonly int _localeId;

    public RecentCatalogue(Handle handle, int localeId)
        => (_handle, _localeId) = (handle, localeId);

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> QueryEvents(Query query)
    {
        var db = _handle.Db();

        var events = QueryEvents(db, query);

        var edges = FindEdges(db, events);

        var pairs = (from ev in events
                from e in edges
                    .LeftJoin(edge => edge.EventId == ev.Id)
                select new LocalizedEdgeCollector<TemporaryEventRelation>.EventEdgePair(ev, e))
            .OrderBy(p => p.Event.Offset);

        var collector = new LocalizedEdgeCollector<TemporaryEventRelation>(ECatalogue.Recent);

        collector.Completed += (_, _) =>
        {
            events.Drop();
            edges.Drop();
        };

        return collector.Collect(pairs);
    }

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> FindAssociations(
        Query eventQuery,
        INode associationFilter)
    {
        var db = _handle.Db();

        var events = QueryEvents(db, eventQuery);

        var edges = FindEdges(db, events);

        var expr = associationFilter.ToExpression<EdgeView>();

        var pairs = (from ev in events
                from e in edges
                    .LeftJoin(edge => edge.EventId == ev.Id)
                where edges.Where(expr).Any(edge => edge.EventId == ev.Id)
                select new LocalizedEdgeCollector<TemporaryEventRelation>.EventEdgePair(ev, e))
            .OrderBy(p => p.Event.Offset);

        var collector = new LocalizedEdgeCollector<TemporaryEventRelation>(ECatalogue.Recent);

        collector.Completed += (_, _) =>
        {
            events.Drop();
            edges.Drop();
        };

        return collector.Collect(pairs);
    }

    TempTable<TemporaryEventRelation> QueryEvents(DataConnection db, Query query)
    {
        var tempTable = db.CreateTempTable<TemporaryEventRelation>("_event");

            db.GetTable<TemporaryEventRelation>()
            .SchemaName("audit")
            .TableName("RecentEventView")
            .Where(r => r.LocaleId == null || r.LocaleId == _localeId)
            .Query(query)
            .Into(tempTable)
            .Insert();

        return tempTable;
    }

    TempTable<EdgeView> FindEdges(DataConnection db, IQueryable<EventView> events)
    {
        var table = CreateTemporaryEdgeTable(db);

        EdgeCte(db, events)
            .Into(table)
            .Insert();

        return table;
    }

    static TempTable<EdgeView> CreateTemporaryEdgeTable(DataConnection db)
    {
        var table = db.CreateTempTable<EdgeView>("_edge");

        using (var cmd = db.Connection.CreateCommand())
        {
            cmd.Transaction = db.Transaction;

            cmd.CommandText = @"CREATE INDEX _edge_EventId_Idx ON _edge(""EventId"")";

            cmd.ExecuteNonQuery();
        }

        return table;
    }

    IQueryable<EdgeView> EdgeCte(DataConnection db, IQueryable<EventView> events)
    {
        var queryable = db.GetCte<EdgeView>(edge =>
        {
            return (
                    from e in events
                    from ta in db.GetTable<TopicAssociationRelation>()
                        .TableName("RecentTopicAssociation")
                        .InnerJoin(a => a.Topic1 == e.TopicId)
                    from l in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic1 == t.Id)
                    from lk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => l.KindId == k.Id)
                    from lres in db.GetTable<TextResourceRelation>()
                        .InnerJoin(res => res.Id == l.TextId)
                    from ltrans in db.GetTable<TranslatedTextRelation>()
                        .LeftJoin(trans => l.Translatable && trans.ResourceId == l.TextId && trans.LocaleId == _localeId)
                    from a in db.GetTable<AssociationRelation>()
                        .InnerJoin(assoc => ta.AssociationId == assoc.Id)
                    from r in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic2 == t.Id)
                    from rk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => r.KindId == k.Id)
                    from rres in db.GetTable<TextResourceRelation>()
                        .InnerJoin(res => res.Id == r.TextId)
                    from rtrans in db.GetTable<TranslatedTextRelation>()
                        .LeftJoin(trans => r.Translatable && trans.ResourceId == r.TextId && trans.LocaleId == _localeId)
                    select new EdgeView
                    {
                        EventId = e.Id,
                        LeftTopicId = ta.Topic1,
                        LeftTopic = ltrans.Translation ?? lres.MsgId,
                        LeftTopicKindId = l.KindId,
                        LeftTopicKind = lk.Name,
                        AssociationId = ta.AssociationId,
                        Association = a.Name,
                        RightTopicId = ta.Topic2,
                        RightTopic = rtrans.Translation ?? rres.MsgId,
                        RightTopicKindId = r.KindId,
                        RightTopicKind = rk.Name
                    }
                )
                .Concat(
                    from ta in db.GetTable<TopicAssociationRelation>()
                        .TableName("RecentTopicAssociation")
                    from a in db.GetTable<AssociationRelation>()
                        .InnerJoin(assoc => ta.AssociationId == assoc.Id)
                    from r in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic2 == t.Id)
                    from rk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => r.KindId == k.Id)
                    from p in edge.InnerJoin(e => e.RightTopicId == ta.Topic1)
                    from rres in db.GetTable<TextResourceRelation>()
                        .InnerJoin(res => res.Id == r.TextId)
                    from rtrans in db.GetTable<TranslatedTextRelation>()
                        .LeftJoin(trans => r.Translatable && trans.ResourceId == r.TextId && trans.LocaleId == _localeId)
                    select new EdgeView
                    {
                        EventId = p.EventId,
                        LeftTopicId = p.RightTopicId,
                        LeftTopic = p.RightTopic,
                        LeftTopicKindId = p.RightTopicKindId,
                        LeftTopicKind = p.RightTopicKind,
                        AssociationId = ta.AssociationId,
                        Association = a.Name,
                        RightTopicId = ta.Topic2,
                        RightTopic = rtrans.Translation ?? rres.MsgId,
                        RightTopicKindId = r.KindId,
                        RightTopicKind = rk.Name
                    }
                );
        });

        return queryable;
    }

    public void Delete(INode filter)
    {
        var db = _handle.Db();
        var util = new DeleteUtil("Recent");

        util.Delete(db, filter);
    }
}