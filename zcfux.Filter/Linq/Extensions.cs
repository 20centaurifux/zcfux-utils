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
namespace zcfux.Filter.Linq;

using System.Linq.Expressions;

public static class Extensions
{
    public static IQueryable<T> Query<T>(this IQueryable<T> self, Query query)
    {
        var queryable = self;

        if (query.Filter?.ToExpression<T>() is { } expr)
        {
            queryable = queryable.Where(expr);
        }

        queryable = queryable.Order(query.Order);
        queryable = queryable.Range(query.Range);

        return queryable;
    }

    public static Expression<Func<T, bool>> ToExpression<T>(this INode self)
    {
        var visitor = new Visitor<T>();

        self.Traverse(visitor);

        return visitor.Expression!;
    }

    public static IQueryable<T> Range<T>(this IQueryable<T> self, Range range)
    {
        var queryable = self;

        if (range.Skip.HasValue)
        {
            queryable = queryable.Skip(range.Skip.Value);
        }

        if (range.Limit.HasValue)
        {
            queryable = queryable.Take(range.Limit.Value);
        }

        return queryable;
    }

    public static IQueryable<T> Order<T>(this IQueryable<T> self, params (string, EDirection)[] properties)
    {
        var queryable = self;

        var exprs = properties
            .Select(t => (NewOrderExpression<T>(t.Item1), t.Item2))
            .ToArray();

        if (exprs.Any())
        {
            var (expr, direction) = exprs[0];

            IOrderedQueryable<T> orderedQueryable;

            if (direction == EDirection.Ascending)
            {
                orderedQueryable = queryable.OrderBy(expr);
            }
            else
            {
                orderedQueryable = queryable.OrderByDescending(expr);
            }

            foreach (var (expr2, direction2) in exprs.Skip(1))
            {
                if (direction2 == EDirection.Ascending)
                {
                    orderedQueryable = orderedQueryable.ThenBy(expr2);
                }
                else
                {
                    orderedQueryable = orderedQueryable.ThenByDescending(expr2);
                }
            }

            queryable = orderedQueryable;
        }

        return queryable;
    }

    static Expression<Func<T, object>> NewOrderExpression<T>(string propertyName)
    {
        var type = typeof(T);
        var property = type.GetProperty(propertyName);
        var parameter = Expression.Parameter(type);
        var access = Expression.Property(parameter, property!);
        var convert = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<T, object>>(convert, parameter);
    }
}