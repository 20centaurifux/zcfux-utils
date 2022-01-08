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

public static class Extensions
{
    public static INode IsNull<T>(this Column<T> self)
        => new Function("null?", self);

    public static INode EqualTo<T>(this Column<T> self, T value)
        => new Function("=", self, new Value(value));

    public static INode NotEqualTo<T>(this Column<T> self, T value)
        => new Function("<>", self, new Value(value));

    public static INode GreaterThan(this Column<int> self, int value)
        => new Function(">", self, new Value(value));

    public static INode GreaterThan(this Column<long> self, long value)
        => new Function(">", self, new Value(value));

    public static INode GreaterThan(this Column<double> self, double value)
        => new Function(">", self, new Value(value));

    public static INode GreaterThan(this Column<DateTime> self, DateTime value)
        => new Function(">", self, new Value(value));

    public static INode GreaterThanOrEqualTo(this Column<int> self, int value)
        => new Function(">=", self, new Value(value));

    public static INode GreaterThanOrEqualTo(this Column<long> self, long value)
        => new Function(">=", self, new Value(value));

    public static INode GreaterThanOrEqualTo(this Column<double> self, double value)
        => new Function(">=", self, new Value(value));

    public static INode GreaterThanOrEqualTo(this Column<DateTime> self, DateTime value)
        => new Function(">=", self, new Value(value));

    public static INode LessThan(this Column<int> self, int value)
        => new Function("<", self, new Value(value));

    public static INode LessThan(this Column<long> self, long value)
        => new Function("<", self, new Value(value));

    public static INode LessThan(this Column<double> self, double value)
        => new Function("<", self, new Value(value));

    public static INode LessThan(this Column<DateTime> self, DateTime value)
        => new Function("<", self, new Value(value));

    public static INode LessThanOrEqualTo(this Column<int> self, int value)
        => new Function("<=", self, new Value(value));

    public static INode LessThanOrEqualTo(this Column<long> self, long value)
        => new Function("<=", self, new Value(value));

    public static INode LessThanOrEqualTo(this Column<double> self, double value)
        => new Function("<=", self, new Value(value));

    public static INode LessThanOrEqualTo(this Column<DateTime> self, DateTime value)
        => new Function("<=", self, new Value(value));

    public static INode Between(this Column<int> self, int from, int to)
        => new Function("between", self, new Value(from), new Value(to));

    public static INode Between(this Column<long> self, long from, long to)
        => new Function("between", self, new Value(from), new Value(to));

    public static INode Between(this Column<double> self, double from, double to)
        => new Function("between", self, new Value(from), new Value(to));

    public static INode Between(this Column<DateTime> self, DateTime from, DateTime to)
        => new Function("between", self, new Value(from), new Value(to));

    public static INode In<T>(this Column<T> self, IEnumerable<T> values)
        => new Function("in", self, new Value(values.ToArray()));

    public static INode In<T>(this Column<T> self, params T[] values)
        => new Function("in", self, new Value(values.ToArray()));

    public static INode StartsWith(this Column<string> self, string text)
        => new Function("starts-with?", self, new Value(text));

    public static INode EndsWith(this Column<string> self, string text)
        => new Function("ends-with?", self, new Value(text));

    public static INode Contains(this Column<string> self, string text)
        => new Function("contains?", self, new Value(text));
}