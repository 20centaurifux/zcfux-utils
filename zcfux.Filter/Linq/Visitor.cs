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
using System.Linq.Expressions;

namespace zcfux.Filter.Linq;

internal class Visitor<T> : IVisitor
{
    readonly Stack<Frame<T>> _stack = new();

    public Expression<Func<T, bool>>? Expression { get; private set; }

    public void BeginFunction(string name)
    {
        _stack.TryPeek(out var parent);

        _stack.Push(new Frame<T>(name, parent));
    }

    public void EndFunction()
    {
        var frame = _stack.Pop();

        var expr = frame.ToExpression();

        if (_stack.TryPeek(out var head))
        {
            head.AddArgument(expr);
        }
        else
        {
            Expression = expr;
        }
    }

    public void Visit(object? value)
        => _stack.Peek().AddArgument(value);
}