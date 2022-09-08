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

public sealed class PasswordChangerTests
{
    sealed class PlaintextChanger : APasswordChanger
    {
        readonly IDictionary<Guid, string> _m = new Dictionary<Guid, string>();

        public void AddUser(Guid guid)
            => _m[guid] = String.Empty;

        protected override bool CanChangePassword(IUser user)
            => _m.ContainsKey(user.Guid);

        protected override void ChangePassword(IUser user, string password)
            => _m[user.Guid] = password;

        public string? Lookup(Guid guid)
        {
            if (_m.TryGetValue(guid, out var password))
            {
                return password;
            }

            return null;
        }
    }

    [Test]
    public void SinglePasswordChanger()
    {
        var changer = new PlaintextChanger();

        var origin = Origin.Random();
        var user = User.Random(origin);
        var password = TestContext.CurrentContext.Random.GetString();

        changer.AddUser(user.Guid);

        Assert.IsTrue(changer.IsChangable(user));

        changer.Change(user, password);

        Assert.AreEqual(password, changer.Lookup(user.Guid));
    }

    [Test]
    public void SinglePasswordChangerWithoutMatch()
    {
        var changer = new PlaintextChanger();

        var origin = Origin.Random();
        var user = User.Random(origin);
        var password = TestContext.CurrentContext.Random.GetString();

        Assert.IsFalse(changer.IsChangable(user));

        Assert.Throws<InvalidOperationException>(() => changer.Change(user, password));

        Assert.IsNull(changer.Lookup(user.Guid));
    }

    [Test]
    public void MultiplePasswordChangers()
    {
        var firstOrigin = Origin.Random();
        var firstUser = User.Random(firstOrigin);

        var firstChanger = new PlaintextChanger();

        firstChanger.AddUser(firstUser.Guid);

        var secondChanger = new PlaintextChanger();

        var secondOrigin = Origin.Random();
        var secondUser = User.Random(secondOrigin);

        secondChanger.AddUser(secondUser.Guid);

        var chain = firstChanger
            .Concat(secondChanger);

        var firstPassword = TestContext.CurrentContext.Random.GetString();

        chain.Change(firstUser, firstPassword);

        var secondPassword = TestContext.CurrentContext.Random.GetString();

        chain.Change(secondUser, secondPassword);

        Assert.IsTrue(chain.IsChangable(firstUser));
        Assert.IsTrue(chain.IsChangable(secondUser));

        Assert.AreEqual(firstPassword, firstChanger.Lookup(firstUser.Guid));
        Assert.AreEqual(secondPassword, secondChanger.Lookup(secondUser.Guid));

        var nonExistingUser = User.Random(firstOrigin);

        Assert.IsFalse(chain.IsChangable(nonExistingUser));
    }
}