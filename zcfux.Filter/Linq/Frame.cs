﻿/***************************************************************************
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
using System.Collections;
using System.Linq.Expressions;

namespace zcfux.Filter.Linq;

sealed class Frame<T>
{
    readonly string _name;
    readonly List<object?> _args = new();
    readonly ParameterExpression _parameter;

    public Frame(string name, Frame<T>? parent)
        => (_name, _parameter) = (name, parent?._parameter
                                        ?? Expression.Parameter(typeof(T), "p"));

    public void AddArgument<V>(V arg)
        => _args.Add(arg);

    public Expression<Func<T, bool>> ToExpression()
    {
        var expr = _name switch
        {
            "not" => Not(),
            "and" or "or" => Logical(),
            _ => Function()
        };

        return expr;
    }

    Expression<Func<T, bool>> Not()
    {
        var child = ArgToExpression() as Expression<Func<T, bool>>;

        var expr = Expression.Not(child!.Body);

        return Expression.Lambda<Func<T, bool>>(expr, _parameter);
    }

    Expression<Func<T, bool>> Logical()
    {
        var queue = new Queue<Expression<Func<T, bool>>>(_args.Cast<Expression<Func<T, bool>>>());

        var left = queue.Dequeue();
        var right = queue.Dequeue();

        Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>, Expression<Func<T, bool>>> make
            = (_name == "or")
                ? Or
                : And;

        var root = make(left, right);

        while (queue.TryDequeue(out right))
        {
            root = make(root, right);
        }

        return root;
    }

    static Expression<Func<T, bool>> Or(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var invoked = Expression.Invoke(right, left.Parameters);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, invoked), left.Parameters);
    }

    static Expression<Func<T, bool>> And(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var invoked = Expression.Invoke(right, left.Parameters);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, invoked), left.Parameters);
    }

    Expression<Func<T, bool>> Function()
    {
        var body = _name switch
        {
            "=" => BinaryExpression(ExpressionType.Equal),
            ">=" => BinaryExpression(ExpressionType.GreaterThanOrEqual),
            ">" => BinaryExpression(ExpressionType.GreaterThan),
            "<=" => BinaryExpression(ExpressionType.LessThanOrEqual),
            "<" => BinaryExpression(ExpressionType.LessThan),
            "<>" => BinaryExpression(ExpressionType.NotEqual),
            "between" => BetweenExpression(),
            "in" => In(),
            "starts-with?" => StartsWith(),
            "ends-with?" => EndsWith(),
            "contains?" => Contains(),
            "null?" => IsNull(),
            _ => throw new InvalidOperationException()
        };

        return Expression.Lambda<Func<T, bool>>(body, _parameter);
    }

    Expression BinaryExpression(ExpressionType type)
    {
        var (left, right) = TwoArgsToExpressions();

        if (IsNullableType(left.Type) && !IsNullableType(right.Type))
        {
            right = Expression.Convert(right, left.Type);
        }
        else if (!IsNullableType(left.Type) && IsNullableType(right.Type))
        {
            left = Expression.Convert(left, right.Type);
        }

        return Expression.MakeBinary(type, left, right);
    }

    static bool IsNullableType(Type t)
        => t.IsGenericType
           && t.GetGenericTypeDefinition() == typeof(Nullable<>);

    Expression ColumnExpression(IColumn column)
        => Expression.Property(_parameter, column.Name);

    static Expression ConstantExpression(object? value)
    {
        var type = typeof(object);

        if (value != null)
        {
            type = value.GetType();
        }

        return Expression.Constant(value, type);
    }

    Expression BetweenExpression()
    {
        var (member, lower, upper) = ThreeArgsToExpressions();

        return Expression.AndAlso(Expression.GreaterThanOrEqual(member, lower), Expression.LessThanOrEqual(member, upper));
    }

    Expression In()
    {
        var type = (_args[0] is IColumn column)
            ? column.Type
            : _args[0]!.GetType()!;

        var listType = typeof(List<>).MakeGenericType(type);

        var l = (IList)Activator.CreateInstance(listType, _args[1])!;

        var haystack = ArgExpression(l);

        var needle = ArgExpression(_args[0]);

        return Expression.Call(haystack, "Contains", null, needle);
    }

    Expression StartsWith()
    {
        var (haystack, needle) = TwoArgsToExpressions();

        return Expression.Call(haystack, "StartsWith", null, needle);
    }

    Expression EndsWith()
    {
        var (haystack, needle) = TwoArgsToExpressions();

        return Expression.Call(haystack, "EndsWith", null, needle);
    }

    Expression Contains()
    {
        var (haystack, needle) = TwoArgsToExpressions();

        return Expression.Call(haystack, "Contains", null, needle);
    }

    Expression IsNull()
    {
        var arg = ArgToExpression();

        return Expression.MakeBinary(ExpressionType.Equal, arg, ConstantExpression(null));
    }

    Expression ArgToExpression()
        => ArgExpression(_args[0]);

    (Expression, Expression) TwoArgsToExpressions()
        => (ArgExpression(_args[0]),
            ArgExpression(_args[1]));

    (Expression, Expression, Expression) ThreeArgsToExpressions()
        => (ArgExpression(_args[0]),
            ArgExpression(_args[1]),
            ArgExpression(_args[2]));

    Expression ArgExpression(object? arg)
    {
        var expr = arg switch
        {
            IColumn column => ColumnExpression(column),
            Expression expression => expression,
            _ => ConstantExpression(arg)
        };

        return expr;
    }
}