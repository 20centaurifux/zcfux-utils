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
using System.Collections;
using System.Text;

namespace zcfux.Filter.Test;

internal sealed class Sexpression : IVisitor
{
    readonly List<object?> _stack = new();

    Sexpression()
    {
    }

    public static string Parse(INode node)
    {
        var sexpr = new Sexpression();

        node.Traverse(sexpr);

        return sexpr.Join();
    }

    string Join()
    {
        var sb = new StringBuilder();

        object? prev = null;

        var token = _stack.ToArray();

        foreach (var t in token)
        {
            if (sb.Length > 0 && prev is not "(" && t is not ")")
            {
                sb.Append(' ');
            }

            sb.Append(t);

            prev = t;
        }

        return sb.ToString().TrimEnd();
    }

    public void BeginFunction(string name)
    {
        _stack.Add("(");
        _stack.Add(name);
    }

    public void EndFunction()
        => _stack.Add(")");

    public void Visit(object? value)
    {
        if (!VisitColumn(value)
            && !VisitEnumerable(value))
        {
            VisitValue(value);
        }
    }

    bool VisitColumn(object? value)
    {
        var visited = false;

        if (value is IColumn column)
        {
            _stack.Add($"[{column.Name}]");

            visited = true;
        }

        return visited;
    }

    bool VisitEnumerable(object? value)
    {
        var visited = false;

        if (value?.GetType() != typeof(string)
            && value is IEnumerable values)
        {
            _stack.Add("(");

            foreach (var v in values)
            {
                _stack.Add(v);
            }

            _stack.Add(")");

            visited = true;
        }

        return visited;
    }

    void VisitValue(object? value)
        => _stack.Add(ConvertValue(value));

    static object? ConvertValue(object? value)
        => (value is string str) ? $"'{str.Replace("'", "\\'")}'" : value;
}