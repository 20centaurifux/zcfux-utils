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
using NUnit.Framework;
using zcfux.Filter.Linq;

namespace zcfux.Filter.Test;

public sealed class LinqTest
{
    record Model(int Id, string? Value);

    static class Columns
    {
        public static readonly Column<int> Id = Column<int>.FromMember();

        public static readonly Column<string> Value = Column<string>.FromMember();
    }

    static readonly Model[] Data =
    {
        new(1, "hello"),
        new(2, null),
        new(3, "world")
    };

    [Test]
    public void IsNull()
    {
        var filter = Columns.Value.IsNull();

        var match = Filter(filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(Data[1], match[0]);
    }

    [Test]
    public void GreaterThanOrEqualTo()
    {
        var filter = Columns.Id.GreaterThanOrEqualTo(2);

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[1], match[0]);
        Assert.AreEqual(Data[2], match[1]);
    }

    [Test]
    public void GreaterThan()
    {
        var filter = Columns.Id.GreaterThan(2);

        var match = Filter(filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(Data[2], match[0]);
    }

    [Test]
    public void EqualTo()
    {
        var filter = Columns.Value.EqualTo("hello");

        var match = Filter(filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(Data[0], match[0]);
    }

    [Test]
    public void NotEqualTo()
    {
        var filter = Columns.Value.NotEqualTo("world");

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[0], match[0]);
        Assert.AreEqual(Data[1], match[1]);
    }

    [Test]
    public void LessThanOrEqualTo()
    {
        var filter = Columns.Id.LessThanOrEqualTo(2);

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[0], match[0]);
        Assert.AreEqual(Data[1], match[1]);
    }

    [Test]
    public void LessThan()
    {
        var filter = Columns.Id.LessThan(2);

        var match = Filter(filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(Data[0], match[0]);
    }

    [Test]
    public void Between()
    {
        var filter = Columns.Id.Between(2, 3);

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[1], match[0]);
        Assert.AreEqual(Data[2], match[1]);
    }

    [Test]
    public void StartsWith()
    {
        Model[] data =
        {
            new(1, "hello"),
            new(2, "world")
        };

        var filter = Columns.Value.StartsWith("he");

        var match = Filter(data, filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(data[0], match[0]);
    }

    [Test]
    public void EndsWith()
    {
        Model[] data =
        {
            new(1, "hello"),
            new(2, "world")
        };

        var filter = Columns.Value.EndsWith("ld");

        var match = Filter(data, filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(data[1], match[0]);
    }

    [Test]
    public void Contains()
    {
        Model[] data =
        {
            new(1, "hello"),
            new(2, "world")
        };

        var filter = Columns.Value.Contains("ll");

        var match = Filter(data, filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(data[0], match[0]);
    }

    static Model[] Filter(INode node)
        => Filter(Data, node);

    static Model[] Filter(IEnumerable<Model> source, INode node)
        => source.Where(node.ToExpression<Model>().Compile())
            .OrderBy(m => m.Id)
            .ToArray();
}