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

namespace zcfux.Filter.Test;

public sealed class QueryBuilderTests
{
    [Test]
    public void Empty()
    {
        var qb = new QueryBuilder();

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.IsEmpty(q.Order);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void Filter()
    {
        var qb = new QueryBuilder()
            .WithFilter(Columns.Id.EqualTo(1));

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsInstanceOf<INode>(q.Filter);
        Assert.IsTrue(q.HasFilter);

        Assert.IsEmpty(q.Order);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }


    [Test]
    public void OrderByString()
    {
        var qb = new QueryBuilder()
            .WithOrderBy("Id");

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.AreEqual("Id", q.Order[0].Item1);
        Assert.AreEqual(EDirection.Ascending, q.Order[0].Item2);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void OrderByColumn()
    {
        var qb = new QueryBuilder()
            .WithOrderBy(Columns.Id);

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.AreEqual("Id", q.Order[0].Item1);
        Assert.AreEqual(EDirection.Ascending, q.Order[0].Item2);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void OrderByDescendingString()
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending("Id");

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.AreEqual("Id", q.Order[0].Item1);
        Assert.AreEqual(EDirection.Descending, q.Order[0].Item2);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void OrderByDescendingColumn()
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(Columns.Id);

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.AreEqual("Id", q.Order[0].Item1);
        Assert.AreEqual(EDirection.Descending, q.Order[0].Item2);

        Assert.IsNull(q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void WithSkip()
    {
        var skip = TestContext.CurrentContext.Random.Next();

        var qb = new QueryBuilder()
            .WithSkip(skip);

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.IsEmpty(q.Order);

        Assert.AreEqual(skip, q.Range.Skip);
        Assert.IsNull(q.Range.Limit);
    }

    [Test]
    public void WithLimit()
    {
        var limit = TestContext.CurrentContext.Random.Next();

        var qb = new QueryBuilder()
            .WithLimit(limit);

        var q = qb.Build();

        Assert.IsInstanceOf<Query>(q);

        Assert.IsNull(q.Filter);
        Assert.IsFalse(q.HasFilter);

        Assert.IsEmpty(q.Order);

        Assert.IsNull(q.Range.Skip);
        Assert.AreEqual(limit, q.Range.Limit);
    }
}