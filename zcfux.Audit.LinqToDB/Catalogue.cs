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
using zcfux.Translation.LinqToDB;

namespace zcfux.Audit.LinqToDB;

sealed class Catalogue : ICatalogue
{
    readonly object _handle;
    readonly bool _archived;
    readonly int _localeId;

    public Catalogue(object handle, ECatalogue catalogue, int localeId)
        => (_handle, _archived, _localeId) = (handle, (catalogue == ECatalogue.Archive), localeId);

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> QueryEvents(Query query)
    {
        var events = _handle
            .Db()
            .GetTable<EventView>()
            .Where(ev => ev.Archived == _archived
                         && (ev.LocaleId == null || ev.LocaleId == _localeId))
            .Query(query)
            .AsEnumerable()
            .Select(ev => ev.ToLocalizedEvent())
            .ToArray();

        foreach (var ev in events)
        {
            var edges = EdgeCte(ev.Id)
                .AsEnumerable()
                .Select(e => e.ToLocalizedEdge());

            yield return (ev, edges);
        }
    }

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> FindAssociation(
        Query eventQuery,
        params INode[] associationFilters)
    {
        var events = FilterEvents(eventQuery.Filter, eventQuery.Order);

        var result = GetFilteredEdges(events, associationFilters);

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

    IEnumerable<ILocalizedEvent> FilterEvents(INode? eventFilter, (string, EDirection)[] order)
    {
        var eventsQuery = _handle
            .Db()
            .GetTable<EventView>()
            .Where(ev => ev.Archived == _archived
                         && (ev.LocaleId == null || ev.LocaleId == _localeId));

        if (eventFilter?.ToExpression<EventView>() is { } expr)
        {
            eventsQuery = eventsQuery.Where(expr);
        }

        var events = eventsQuery
            .Order(order)
            .AsEnumerable()
            .Select(ev => ev.ToLocalizedEvent());

        return events;
    }

    IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> GetFilteredEdges(
        IEnumerable<ILocalizedEvent> events,
        INode[] associationFilters)
    {
        foreach (var ev in events)
        {
            if (ev.Topic is not null)
            {
                var view = EdgeCte(ev.Id)
                    .ToArray();

                var match = false;

                for (var i = 0; !match && (i < associationFilters.Length); i++)
                {
                    var associationFilter = associationFilters[i];

                    var expr = associationFilter.ToExpression<EdgeView>();

                    match = view.AsQueryable().Any(expr);

                    if (match)
                    {
                        yield return (ev, view.Select(e => e.ToLocalizedEdge()));
                    }
                }
            }
        }
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
}