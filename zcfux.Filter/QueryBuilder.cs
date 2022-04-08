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
namespace zcfux.Filter;

public sealed class QueryBuilder
{
    INode? _filter;
    readonly List<(string, EDirection)> _columns = new List<(string, EDirection)>();
    int? _skip;
    int? _limit;

    public QueryBuilder()
    {
    }

    public static Query All()
        => new QueryBuilder().Build();

    QueryBuilder(QueryBuilder other)
    {
        _filter = other._filter;
        _columns = new List<(string, EDirection)>(other._columns);
        (_skip, _limit) = (other._skip, other._limit);
    }

    public static QueryBuilder FromQuery(Query query)
    {
        var qb = new QueryBuilder
        {
            _filter = query.Filter,
            _skip = query.Range.Skip,
            _limit = query.Range.Limit
        };

        qb._columns.AddRange(query.Order);

        return qb;
    }

    public QueryBuilder WithFilter(INode filter)
    {
        var qb = new QueryBuilder(this);

        qb._filter = filter;

        return qb;
    }

    public QueryBuilder WithOrderBy(IColumn column)
        => WithOrderBy(column.Name);

    public QueryBuilder WithOrderBy(string column)
    {
        var qb = new QueryBuilder(this);

        qb._columns.Add((column, EDirection.Ascending));

        return qb;
    }

    public QueryBuilder WithOrderByDescending(IColumn column)
        => WithOrderByDescending(column.Name);

    public QueryBuilder WithOrderByDescending(string column)
    {
        var qb = new QueryBuilder(this);

        qb._columns.Add((column, EDirection.Descending));

        return qb;
    }

    public QueryBuilder WithSkip(int? skip)
    {
        var qb = new QueryBuilder(this)
        {
            _skip = skip
        };

        return qb;
    }

    public QueryBuilder WithLimit(int? limit)
    {
        var qb = new QueryBuilder(this)
        {
            _limit = limit
        };

        return qb;
    }

    public Query Build()
    {
        var query = new Query
        {
            Filter = _filter,
            Order = _columns.ToArray(),
            Range = new Range(_skip, _limit)
        };

        return query;
    }
}