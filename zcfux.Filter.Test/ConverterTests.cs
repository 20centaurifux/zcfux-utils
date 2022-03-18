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
using zcfux.Filter.Convert;

namespace zcfux.Filter.Test;

public sealed class ConverterTests
{
    [Test]
    public void ValueConverter()
    {
        var node = Columns.Id.Between(2, 4);

        var converter = new ValueConverter(v =>
        {
            var result = v;

            if (v is long n)
            {
                result = n * 2;
            }

            return result;
        });

        var transformed = converter.Convert(node);

        var sexpr = Sexpression.Parse(transformed);

        Assert.AreEqual("(between [Id] 4 8)", sexpr);
    }
}