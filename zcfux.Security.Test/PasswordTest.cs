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

namespace zcfux.Security.Test;

public sealed class PasswordTest
{
    [Test]
    public void Hash()
    {
        var plain = TestContext.CurrentContext.Random.GetString();

        var (hash1, salt1) = Password.Hash(plain);

        var (hash2, salt2) = Password.Hash(plain);

        Assert.AreNotEqual(hash1, hash2);
        Assert.AreNotEqual(salt1, salt2);

        var hash3 = Password.Hash(plain, salt1);

        Assert.AreEqual(hash1, hash3);
    }
}