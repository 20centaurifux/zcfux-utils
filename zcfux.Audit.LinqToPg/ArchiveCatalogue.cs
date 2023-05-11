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

namespace zcfux.Audit.LinqToPg;

sealed class ArchiveCatalogue : ICatalogue
{
    readonly Handle _handle;
    readonly int _localeId;

    public ArchiveCatalogue(Handle handle, int localeId)
        => (_handle, _localeId) = (handle, localeId);

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> QueryEvents(Query query)
    {
        var db = _handle.Db();

        var pairs = from ev in QueryEvents(db, query)
            from e in db.GetTable<EdgeView>()
                .TableName("ArchivedEdgeView")
                .SchemaName("audit")
                .Where(e => e.LocaleId == null || e.LocaleId == _localeId)
                .LeftJoin(edge => edge.EventId == ev.Id)
            select new LocalizedEdgeCollector<EventView>.EventEdgePair(ev, e);

        pairs = OrderBy(pairs, query.Order);

        var collector = new LocalizedEdgeCollector<EventView>(ECatalogue.Archive);

        return collector.Collect(pairs);
    }

    public IEnumerable<(ILocalizedEvent, IEnumerable<ILocalizedEdge>)> FindAssociation(
        Query eventQuery,
        params INode[] associationFilters)
    {
        var db = _handle.Db();

        var expr = ExpressionBuilder.Or(
            associationFilters
                .Select(f => f.ToExpression<EdgeView>())
                .ToArray());


        var edges = db.GetTable<EdgeView>()
            .TableName("ArchivedEdgeView")
            .SchemaName("audit")
            .Where(e => e.LocaleId == _localeId);

        var pairs = from ev in QueryEvents(db, eventQuery)
            from e in edges
                .LeftJoin(edge => edge.EventId == ev.Id)
            where edges.Where(expr).Any(edge => edge.EventId == ev.Id)
            select new LocalizedEdgeCollector<EventView>.EventEdgePair(ev, e);

        pairs = OrderBy(pairs, eventQuery.Order);

        var collector = new LocalizedEdgeCollector<EventView>(ECatalogue.Archive);

        return collector.Collect(pairs);
    }

    static IQueryable<LocalizedEdgeCollector<EventView>.EventEdgePair> OrderBy(
        IQueryable<LocalizedEdgeCollector<EventView>.EventEdgePair> queryable,
        (string, EDirection)[] columns)
    {
        if (columns.Any())
        {
            IOrderedEnumerable<LocalizedEdgeCollector<EventView>.EventEdgePair> orderedQueryable;

            var (column1, direction1) = columns.First();

            var expr = NewOrderExpr(column1);

            if (direction1 == EDirection.Ascending)
            {
                orderedQueryable = queryable.AsEnumerable().OrderBy(expr);
            }
            else
            {
                orderedQueryable = queryable.AsEnumerable().OrderByDescending(expr);
            }

            foreach (var (column2, direction2) in columns.Skip(1))
            {
                expr = NewOrderExpr(column2);

                if (direction2 == EDirection.Ascending)
                {
                    orderedQueryable = orderedQueryable.ThenBy(expr);
                }
                else
                {
                    orderedQueryable = orderedQueryable.ThenByDescending(expr);
                }
            }

            queryable = orderedQueryable.AsQueryable();
        }

        return queryable;
    }

    static Func<LocalizedEdgeCollector<EventView>.EventEdgePair, dynamic> NewOrderExpr(string column)
        => column switch
        {
            "Id" => p => p.Event.Id,
            "CreatedAt" => p => p.Event.CreatedAt,
            "Severity" => p => p.Event.Severity,
            "KindId" => p => p.Event.KindId,
            "Kind" => p => p.Event.Kind,
            "DisplayName" => p => p.Event.DisplayName!,
            _ => throw new ArgumentException($"Unexpected column: `{column}'")
        };

    IQueryable<EventView> QueryEvents(DataConnection db, Query query)
        => db.GetTable<EventView>()
            .SchemaName("audit")
            .TableName("ArchivedEventView")
            .Where(ev => ev.LocaleId == null || ev.LocaleId == _localeId)
            .Query(query);

    public void Delete(INode filter)
    {
        var db = _handle.Db();
        var util = new DeleteUtil("Archived");

        util.Delete(db, filter);

        using (var cmd = db.Connection.CreateCommand())
        {
            cmd.Transaction = db.Transaction;

            cmd.CommandText = @"REFRESH MATERIALIZED VIEW CONCURRENTLY audit.""ArchivedEventView""";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"REFRESH MATERIALIZED VIEW CONCURRENTLY audit.""ArchivedEdgeView""";

            cmd.ExecuteNonQuery();
        }
    }
}