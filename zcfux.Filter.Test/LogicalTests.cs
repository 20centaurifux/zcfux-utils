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

public sealed class LogicalTests
{
    [Test]
    public void Not()
    {
        var node = Logical.Not(Columns.Id.Between(23, 42));

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(not (between [Id] 23 42))", sexpr);
    }

    [Test]
    public void And()
    {
        var node = Logical.And(Columns.Id.GreaterThan(23), Columns.Id.LessThan(42));

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(and (> [Id] 23) (< [Id] 42))", sexpr);
    }

    [Test]
    public void Or()
    {
        var node = Logical.Or(Columns.Text.EqualTo("foo"), Columns.Text.EqualTo("bar"));

        var sexpr = Sexpression.Parse(node);

        Assert.AreEqual("(or (= [Text] 'foo') (= [Text] 'bar'))", sexpr);
    }
}