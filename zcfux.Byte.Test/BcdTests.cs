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

namespace zcfux.Byte.Test;

public sealed class BcdTests
{
    [Test]
    public void ParseAscii()
    {
        Bcd.ParseAscii("1337", out var bytes);

        Assert.AreEqual(bytes[0], 0x13);
        Assert.AreEqual(bytes[1], 0x37);
    }

    [Test]
    public void ParseInvalidAscii()
    {
        Assert.Throws<ArgumentException>(() => Bcd.ParseAscii("1337a", out var bytes));
    }

    [Test]
    public void ToAscii()
    {
        var bytes = new byte[] { 0x13, 0x37 };

        var ascii = Bcd.ToAscii(bytes);

        Assert.AreEqual("1337", ascii);
    }

    [Test]
    public void ToIntegral()
    {
        var n = Bcd.ToIntegral(new byte[] { 0x13, 0x37 });

        Assert.AreEqual(1337, n);
    }

    [Test]
    public void ToIntegralWithOffsetAndLength()
    {
        var n = Bcd.ToIntegral(new byte[] { 0x13, 0x37 }, 1, 1);

        Assert.AreEqual(37, n);
    }
}