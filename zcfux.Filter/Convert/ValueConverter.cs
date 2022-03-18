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
namespace zcfux.Filter.Convert;

public class ValueConverter : IConverter
{
    sealed class Visitor : IVisitor
    {
        readonly Stack<Frame> _stack = new();
        readonly Func<object?, object?> _fn;
        INode? _root;

        public Visitor(Func<object?, object?> fn)
            => _fn = fn;

        public INode Root => _root!;

        public void Visit(object? value)
        {
            var converted = _fn(value);

            _stack.Peek().AddArgument(new Value(converted));
        }

        public void BeginFunction(string name)
            => _stack.Push(new Frame(name));

        public void EndFunction()
        {
            var frame = _stack.Pop();

            var expr = frame.ToNode();

            if (_stack.TryPeek(out var head))
            {
                head.AddArgument(expr);
            }
            else
            {
                _root = expr;
            }
        }
    }

    readonly Func<object?, object?> _fn;

    public ValueConverter(Func<object?, object?> converter)
        => _fn = converter;

    public INode Convert(INode node)
    {
        var visitor = new Visitor(_fn);

        node.Traverse(visitor);

        return visitor.Root;
    }
}