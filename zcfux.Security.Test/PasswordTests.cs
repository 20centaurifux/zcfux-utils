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

public sealed class PasswordTests
{
    [Test]
    public void Hash()
    {
        var plain = TestContext.CurrentContext.Random.GetString();

        var (algorithm, hash1, salt1) = Password.ComputedHash(plain);

        var (_, hash2, salt2) = Password.ComputedHash(algorithm, plain);

        Assert.AreNotEqual(hash1, hash2);
        Assert.AreNotEqual(salt1, salt2);

        var (_, hash3, _) = Password.ComputedHash(algorithm, plain, salt1);

        Assert.AreEqual(hash1, hash3);
    }
}