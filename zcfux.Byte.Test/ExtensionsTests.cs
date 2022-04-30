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

public sealed class ExtensionsTests
{
    [Test]
    public void GetBytes()
    {
        const string text = "hello world";

        var bytes = text.GetBytes();

        Assert.AreEqual(text.Length, bytes.Length);

        for (var i = 0; i < text.Length; i++)
        {
            Assert.AreEqual(text[i], bytes[i]);
        }
    }

    [Test]
    public void ToHex()
    {
        var bytes = new byte[] { 35, 66, 171, 205, 239 };

        var hex = bytes.ToHex();

        Assert.AreEqual("2342ABCDEF", hex);
    }

    [Test]
    public void FromHex()
    {
        const string text = "2342ABCDEF";

        var bytes = text.FromHex();

        Assert.IsTrue(new byte[] { 35, 66, 171, 205, 239 }.SequenceEqual(bytes));
    }

    [Test]
    public void FromInvalidHex()
    {
        Assert.That(() => "A".FromHex(), Throws.Exception);

        Assert.That(() => "BCDEFG".FromHex(), Throws.Exception);
    }

    [Test]
    public void BytesToMemoryStream()
    {
        var bytes = new byte[20];
        
        TestContext.CurrentContext.Random.NextBytes(bytes);

        var ms = bytes.ToMemoryStream();
        
        Assert.AreEqual(bytes, ms.ToArray());
    }
    
    [Test]
    public void StringToMemoryStream()
    {
        var text = TestContext.CurrentContext.Random.GetString();
        
        var ms = text.ToMemoryStream();
        
        Assert.AreEqual(text.GetBytes(), ms.ToArray());
    }
}