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

namespace zcfux.User.Test;

public sealed class PasswordCheckTests
{
    sealed class PlaintextCheck : APasswordCheck
    {
        readonly IDictionary<Guid, string> _m = new Dictionary<Guid, string>();

        public void AddPassword(Guid guid, string password)
            => _m[guid] = password;

        protected override bool CanCheckPassword(IUser user)
            => _m.ContainsKey(user.Guid);

        protected override bool PasswordIsCorrect(IUser user, string password)
            => _m[user.Guid].Equals(password);
    }

    [Test]
    public void SinglePasswordCheck()
    {
        var check = new PlaintextCheck();

        var origin = Origin.Random();
        var user = User.Random(origin);
        var password = TestContext.CurrentContext.Random.GetString();

        check.AddPassword(user.Guid, password);

        Assert.IsTrue(check.Check(user, password));

        var wrongPassword = TestContext.CurrentContext.Random.GetString();

        Assert.IsFalse(check.Check(user, wrongPassword));
    }

    [Test]
    public void SinglePasswordCheckWithoutMatch()
    {
        var check = new PlaintextCheck();

        var origin = Origin.Random();
        var user = User.Random(origin);
        var password = TestContext.CurrentContext.Random.GetString();

        Assert.IsFalse(check.Check(user, password));
    }

    [Test]
    public void MultiplePasswordChecks()
    {
        var firstCheck = new PlaintextCheck();

        var firstOrigin = Origin.Random();
        var firstUser = User.Random(firstOrigin);
        var firstPassword = TestContext.CurrentContext.Random.GetString();

        firstCheck.AddPassword(firstUser.Guid, firstPassword);

        var secondCheck = new PlaintextCheck();

        var secondOrigin = Origin.Random();
        var secondUser = User.Random(secondOrigin);
        var secondPassword = TestContext.CurrentContext.Random.GetString();

        secondCheck.AddPassword(secondUser.Guid, secondPassword);

        var chain = firstCheck
            .Concat(secondCheck);

        Assert.IsTrue(chain.Check(firstUser, firstPassword));
        Assert.IsFalse(chain.Check(firstUser, secondPassword));
        Assert.IsTrue(chain.Check(secondUser, secondPassword));
        Assert.IsFalse(chain.Check(secondUser, firstPassword));
    }
}