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
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.Audit.LinqToDB;

internal sealed class Catalogue : ICatalogue
{
    readonly object _handle;
    readonly bool _archived;

    public Catalogue(object handle, ECatalogue catalogue)
        => (_handle, _archived) = (handle, (catalogue == ECatalogue.Archive));

    public IEnumerable<(IEvent, IEnumerable<IEdge>)> QueryEvents(Query query)
    {
        var events = _handle
            .Db()
            .GetTable<EventView>()
            .Where(v => v.Archived == _archived)
            .Query(query)
            .AsEnumerable()
            .Select(ToEvent)
            .ToArray();

        foreach (var ev in events)
        {
            var edges = EdgeCte(ev.Id)
                .AsEnumerable()
                .Select(ToEdge);

            yield return (ev, edges);
        }
    }

    public IEnumerable<(IEvent, IEnumerable<IEdge>)> FindAssociations(Query eventQuery, INode associationFilter)
    {
        var events = FilterEvents(eventQuery.Filter, eventQuery.Order);

        var result = GetFilteredEdges(events, associationFilter);

        if (eventQuery.Range.Skip.HasValue)
        {
            result = result.Skip(eventQuery.Range.Skip.Value);
        }

        if (eventQuery.Range.Limit.HasValue)
        {
            result = result.Take(eventQuery.Range.Limit.Value);
        }

        return result;
    }

    IEnumerable<IEvent> FilterEvents(INode? eventFilter, (string, EDirection)[] order)
    {
        var eventsQuery = _handle
            .Db()
            .GetTable<EventView>()
            .Where(v => v.Archived == _archived);

        if (eventFilter?.ToExpression<EventView>() is { } expr)
        {
            eventsQuery = eventsQuery.Where(expr);
        }

        var events = eventsQuery
            .Order(order)
            .AsEnumerable()
            .Select(ToEvent);

        return events;
    }

    IEnumerable<(IEvent, IEnumerable<IEdge>)> GetFilteredEdges(IEnumerable<IEvent> events, INode associationFilter)
    {
        var expr = associationFilter.ToExpression<EdgeView>();

        foreach (var ev in events)
        {
            if (ev.Topic is { })
            {
                var view = EdgeCte(ev.Id)
                    .ToArray();

                if (view.AsQueryable().Any(expr))
                {
                    yield return (ev, view.Select(ToEdge));
                }
            }
        }
    }

    public void Delete(INode filter)
    {
        DeleteEvents(filter);
        DeleteOrphans();
    }

    void DeleteEvents(INode filter)
    {
        var expr = filter.ToExpression<EventView>();

        var events = _handle
            .Db()
            .GetTable<EventView>()
            .Where(e => e.Archived == _archived)
            .Where(expr)
            .ToArray();

        foreach (var ev in events)
        {
            if (ev.TopicId is { } topicId)
            {
                DeleteAssociations(topicId);
            }

            _handle
                .Db()
                .GetTable<Event>()
                .Where(e => e.Id == ev.Id)
                .Delete();
        }
    }

    void DeleteAssociations(long rootId)
    {
        var db = _handle.Db();

        var associations = db.GetCte<TopicAssociationRelation>(cte =>
        {
            return (
                    from ta in db.GetTable<TopicAssociationRelation>()
                    where ta.Topic1 == rootId
                    select ta
                )
                .Concat(
                    from ta in db.GetTable<TopicAssociationRelation>()
                    from p in cte.InnerJoin(x => x.Topic2 == ta.Topic1)
                    select ta
                );
        }).ToArray();

        foreach (var association in associations)
        {
            _handle
                .Db()
                .GetTable<TopicAssociationRelation>()
                .Where(a => a.Id == association.Id)
                .Delete();
        }
    }

    void DeleteOrphans()
    {
        var queryable = from t in _handle.Db().GetTable<TopicRelation>()
            where !_handle.Db().GetTable<TopicAssociationRelation>()
                .Any(a => a.Topic1 == t.Id || a.Topic2 == t.Id)
            select t;

        queryable.Delete();
    }

    Event ToEvent(EventView view)
    {
        var ev = new Event
        {
            Id = view.Id,
            Kind = new EventKind(view.KindId, view.Kind),
            Severity = view.Severity,
            CreatedAt = view.CreatedAt,
            Archived = _archived
        };

        if (view.TopicId.HasValue)
        {
            ev.Topic = new Topic
            {
                Id = view.TopicId.Value,
                DisplayName = view.DisplayName!,
                Kind = new TopicKind(view.TopicKindId, view.TopicKind)
            };
        }

        return ev;
    }

    IQueryable<EdgeView> EdgeCte(long eventId)
    {
        var db = _handle.Db();

        var queryable = db.GetCte<EdgeView>(edge =>
        {
            return (
                    from e in db.GetTable<EventRelation>()
                    where e.Id == eventId
                    from ta in db.GetTable<TopicAssociationRelation>()
                        .InnerJoin(a => a.Topic1 == e.TopicId)
                    from l in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic1 == t.Id)
                    from lk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => l.KindId == k.Id)
                    from a in db.GetTable<AssociationRelation>()
                        .InnerJoin(assoc => ta.AssociationId == assoc.Id)
                    from r in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic2 == t.Id)
                    from rk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => r.KindId == k.Id)
                    select new EdgeView
                    {
                        EventId = e.Id,
                        LeftTopicId = ta.Topic1,
                        LeftTopic = l.DisplayName,
                        LeftTopicKindId = l.KindId,
                        LeftTopicKind = lk.Name,
                        AssociationId = ta.AssociationId,
                        Association = a.Name,
                        RightTopicId = ta.Topic2,
                        RightTopic = r.DisplayName,
                        RightTopicKindId = r.KindId,
                        RightTopicKind = rk.Name
                    }
                )
                .Concat(
                    from ta in db.GetTable<TopicAssociationRelation>()
                    from a in db.GetTable<AssociationRelation>()
                        .InnerJoin(assoc => ta.AssociationId == assoc.Id)
                    from r in db.GetTable<TopicRelation>()
                        .InnerJoin(t => ta.Topic2 == t.Id)
                    from rk in db.GetTable<TopicKindRelation>()
                        .InnerJoin(k => r.KindId == k.Id)
                    from p in edge.InnerJoin(e => e.RightTopicId == ta.Topic1)
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
                        RightTopic = r.DisplayName,
                        RightTopicKindId = r.KindId,
                        RightTopicKind = rk.Name
                    }
                );
        });

        return queryable;
    }

    static IEdge ToEdge(EdgeView view)
        => new Edge
        {
            Left = new Topic
            {
                Id = view.LeftTopicId,
                Kind = new TopicKind(view.LeftTopicKindId, view.LeftTopicKind),
                DisplayName = view.LeftTopic
            },
            Association = new AssociationRelation
            {
                Id = view.AssociationId,
                Name = view.Association
            },
            Right = new Topic
            {
                Id = view.RightTopicId,
                Kind = new TopicKind(view.RightTopicKindId, view.RightTopicKind),
                DisplayName = view.RightTopic
            }
        };
}