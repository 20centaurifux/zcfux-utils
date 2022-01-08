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

public sealed class FunctionTest
{
    [Test]
    public void IsNull()
    {
        var node = Columns.Id.IsNull();

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(null? [Id])", sexpr);
    }

    [Test]
    public void GreaterThanOrEqualTo()
    {
        var node = Columns.Id.GreaterThanOrEqualTo(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(>= [Id] 23)", sexpr);
    }

    [Test]
    public void GreaterThan()
    {
        var node = Columns.Id.GreaterThan(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(> [Id] 23)", sexpr);
    }

    [Test]
    public void LessThanOrEqualTo()
    {
        var node = Columns.Id.LessThanOrEqualTo(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(<= [Id] 23)", sexpr);
    }

    [Test]
    public void LessThan()
    {
        var node = Columns.Id.LessThan(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(< [Id] 23)", sexpr);
    }

    [Test]
    public void EqualTo()
    {
        var node = Columns.Id.EqualTo(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(= [Id] 23)", sexpr);
    }

    [Test]
    public void NotEqualTo()
    {
        var node = Columns.Id.NotEqualTo(23);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(<> [Id] 23)", sexpr);
    }

    [Test]
    public void Between()
    {
        var node = Columns.Id.Between(23, 42);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(between [Id] 23 42)", sexpr);
    }

    [Test]
    public void In()
    {
        var node = Columns.Id.In(23, 42);

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(in [Id] (23 42))", sexpr);
    }

    [Test]
    public void StartsWith()
    {
        var node = Columns.Text.StartsWith("foo");

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(starts-with? [Text] 'foo')", sexpr);
    }

    [Test]
    public void EndsWith()
    {
        var node = Columns.Text.EndsWith("bar");

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(ends-with? [Text] 'bar')", sexpr);
    }

    [Test]
    public void Contains()
    {
        var node = Columns.Text.Contains("foobar");

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(contains? [Text] 'foobar')", sexpr);
    }
}