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

namespace zcfux.Mail.Test;

public class AddressTests
{
    [Test]
    public void ParseAddressWithDisplayName()
    {
        var address = Address.FromString("Alice <alice@example.org>");

        Assert.AreEqual("Alice", address.DisplayName);
        Assert.AreEqual("alice@example.org", address.MailAddress);
    }

    [Test]
    public void ParseAddressWithoutDisplayName()
    {
        var address = Address.FromString("alice@example.org");

        Assert.IsNull(address.DisplayName);
        Assert.AreEqual("alice@example.org", address.MailAddress);
    }

    [Test]
    public void ParseInvalidAddress()
    {
        var addresses = new[]
        {
            "alice",
            "alice<alice@example.org",
            "alice alice@example.org>",
            "alice@example.org alice"
        };

        foreach (var address in addresses)
        {
            Assert.That(() =>
            {
                Address.FromString(address);
            }, Throws.Exception);
        }
    }
}