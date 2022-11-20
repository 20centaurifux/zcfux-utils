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

public sealed class LinqTests
{
    sealed record Model(int Id, string? Value);

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

    [Test]
    public void And()
    {
        var filter = Logical.And(
            Columns.Id.EqualTo(1),
            Columns.Value.EqualTo("hello"));

        var match = Filter(filter);

        Assert.AreEqual(1, match.Length);
        Assert.AreEqual(Data[0], match[0]);
    }

    [Test]
    public void Or()
    {
        var filter = Logical.Or(
            Columns.Id.EqualTo(1),
            Columns.Value.EqualTo("world"));

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[0], match[0]);
        Assert.AreEqual(Data[2], match[1]);
    }

    [Test]
    public void Not()
    {
        var filter = Logical.Not(
            Columns.Value.IsNull());

        var match = Filter(filter);

        Assert.AreEqual(2, match.Length);
        Assert.AreEqual(Data[0], match[0]);
        Assert.AreEqual(Data[2], match[1]);
    }

    static Model[] Filter(INode node)
        => Filter(Data, node);

    static Model[] Filter(IEnumerable<Model> source, INode node)
        => source.Where(node.ToExpression<Model>().Compile())
            .OrderBy(m => m.Id)
            .ToArray();

    [Test]
    public void RangeAll()
    {
        var coll = new[] { 1, 2, 3, 4, 5 };

        var result = coll.AsQueryable()
            .Range(Range.All)
            .ToArray();

        Assert.IsTrue(coll.SequenceEqual(result));
    }

    [Test]
    public void RangeSkip()
    {
        var coll = new[] { 1, 2, 3, 4, 5 };

        var result = coll.AsQueryable()
            .Range(new Range(skip: 3))
            .ToArray();

        Assert.IsTrue(coll.Skip(3).SequenceEqual(result));
    }

    [Test]
    public void RangeLimit()
    {
        var coll = new[] { 1, 2, 3, 4, 5 };

        var result = coll.AsQueryable()
            .Range(new Range(limit: 3))
            .ToArray();

        Assert.IsTrue(coll.Take(3).SequenceEqual(result));
    }

    [Test]
    public void RangeSkipAndLimit()
    {
        var coll = new[] { 1, 2, 3, 4, 5 };

        var result = coll.AsQueryable()
            .Range(new Range(skip: 2, limit: 2))
            .ToArray();

        Assert.IsTrue(coll.Skip(2).Take(2).SequenceEqual(result));
    }

    [Test]
    public void OrderAscending()
    {
        var coll = new[]
        {
            new Model(2, "hello"),
            new Model(1, "world")
        };

        var result = coll.AsQueryable()
            .Order(("Id", EDirection.Ascending))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual(2, result[1].Id);
    }

    [Test]
    public void OrderDescending()
    {
        var coll = new[]
        {
            new Model(1, "hello"),
            new Model(2, "world")
        };

        var result = coll.AsQueryable()
            .Order(("Id", EDirection.Descending))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(2, result[0].Id);
        Assert.AreEqual(1, result[1].Id);
    }

    [Test]
    public void OrderMultiple()
    {
        var coll = new[]
        {
            new Model(1, "a"),
            new Model(1, "b"),
            new Model(2, "d"),
            new Model(2, "c")
        };

        var result = coll.AsQueryable()
            .Order(("Value", EDirection.Descending))
            .Order(("Id", EDirection.Ascending))
            .ToArray();

        Assert.AreEqual(4, result.Length);

        Assert.AreEqual(1, result[0].Id);
        Assert.AreEqual("b", result[0].Value);

        Assert.AreEqual(1, result[1].Id);
        Assert.AreEqual("a", result[1].Value);

        Assert.AreEqual(2, result[2].Id);
        Assert.AreEqual("d", result[2].Value);

        Assert.AreEqual(2, result[3].Id);
        Assert.AreEqual("c", result[3].Value);
    }

    [Test]
    public void Query()
    {
        var qb = new QueryBuilder()
            .WithFilter(Columns.Id.Between(2, 5))
            .WithOrderBy(Columns.Id)
            .WithLimit(2)
            .WithSkip(1);

        var q = qb.Build();

        var coll = new[]
        {
            new Model(6, "f"),
            new Model(5, "e"),
            new Model(4, "d"),
            new Model(3, "c"),
            new Model(2, "b"),
            new Model(1, "a")
        };

        var result = coll.AsQueryable()
            .Query(q)
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(3, result[0].Id);
        Assert.AreEqual(4, result[1].Id);
    }
}