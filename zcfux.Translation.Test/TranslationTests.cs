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
using zcfux.Translation.Data;

namespace zcfux.Translation.Test;

public sealed class TranslationTests
{
    [Test]
    public void NoSetup()
    {
        var t = new Translation();

        var translation = t.Translate("foo", "bar");

        Assert.IsNull(translation);
    }

    [Test]
    public void Translate()
    {
        var t = new Translation();

        t.Setup(new Dictionary<ITextResource, string>
        {
            { new TextResource(1, new Category(1, "foo"), "bar"), "baz" }
        });

        var translation = t.Translate("foo", "bar");

        Assert.AreEqual("baz", translation);
    }

    [Test]
    public void KeyNotFound()
    {
        var t = new Translation();

        t.Setup(new Dictionary<ITextResource, string>
        {
            { new TextResource(1, new Category(1, "foo"), "bar"), "baz" }
        });

        var translation = t.Translate("---foo", "bar");

        Assert.IsNull(translation);
    }

    [Test]
    public void LocaleNotFound()
    {
        var t = new Translation();

        t.Setup(new Dictionary<ITextResource, string>
        {
            { new TextResource(1, new Category(1, "foo"), "bar"), "baz" }
        });

        var translation = t.Translate("foo", "----bar");

        Assert.IsNull(translation);
    }
}