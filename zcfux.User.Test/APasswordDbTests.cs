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
using zcfux.Data;
using zcfux.Security;
using zcfux.User.Password;

namespace zcfux.User.Test;

public abstract class APasswordDbTests
{
    IEngine _engine = null!;
    IUserDb _userDb = null!;
    IPasswordDb _passwordDb = null!;

    sealed class PasswordCheck : APasswordDbCheck
    {
        readonly int _originId;

        public PasswordCheck(object handle, IPasswordDb db, int originId)
            : base(handle, db)
            => _originId = originId;

        protected override bool CanCheckPassword(IUser user)
            => user.Origin.Id == _originId;
    }

    sealed class PasswordChanger : APasswordDbChanger
    {
        readonly int _format;
        readonly PasswordHasher _hasher;

        public PasswordChanger(object handle, IPasswordDb db, int format, PasswordHasher hasher)
            : base(handle, db)
            => (_format, _hasher) = (format, hasher);

        protected override int Format
            => _format;

        protected override bool CanChangePassword(IUser user)
            => true;

        protected override (byte[], byte[]) ComputeHashAndSalt(string password)
        {
            var salt = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(salt);

            var hash = _hasher.ComputeHash(password, salt);

            return (hash, salt);
        }
    }

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _userDb = CreateUserDb();
        _passwordDb = CreatePasswordDb();
    }

    [Test]
    public void SetAndGetPassword()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);

            var (hash, salt, format) = RandomPassword();

            _passwordDb.Set(t.Handle, user.Guid, hash, salt, format);

            var password = _passwordDb.TryGet(t.Handle, user.Guid);

            Assert.NotNull(password);
            Assert.IsTrue(hash.SequenceEqual(password!.Hash));
            Assert.IsTrue(salt.SequenceEqual(password.Salt));
            Assert.AreEqual(format, password.Format);
        }
    }

    [Test]
    public void OverwritePassword()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);

            var (hash, salt, format) = RandomPassword();

            _passwordDb.Set(t.Handle, user.Guid, hash, salt, format);

            (hash, salt, format) = RandomPassword();

            _passwordDb.Set(t.Handle, user.Guid, hash, salt, format);

            var password = _passwordDb.TryGet(t.Handle, user.Guid);

            Assert.NotNull(password);
            Assert.IsTrue(hash.SequenceEqual(password!.Hash));
            Assert.IsTrue(salt.SequenceEqual(password.Salt));
            Assert.AreEqual(format, password.Format);
        }
    }

    [Test]
    public void SetPasswordOfNonExistingUser()
    {
        using (var t = _engine.NewTransaction())
        {
            var (hash, salt, format) = RandomPassword();

            Assert.Throws<NotFoundException>(() => _passwordDb.Set(t.Handle, Guid.NewGuid(), hash, salt, format));
        }
    }

    [Test]
    public void DeletePassword()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);

            var (hash, salt, format) = RandomPassword();

            _passwordDb.Set(t.Handle, user.Guid, hash, salt, format);
            _passwordDb.Delete(t.Handle, user.Guid);

            var password = _passwordDb.TryGet(t.Handle, user.Guid);

            Assert.Null(password);
        }
    }

    [Test]
    public void DeleteNonExistingPassword()
    {
        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() => _passwordDb.Delete(t.Handle, Guid.NewGuid()));
        }
    }

    [Test]
    public void SinglePasswordCheck()
    {
        using (var t = _engine.NewTransaction())
        {
            var user = CreateUser(t.Handle);

            var format = TestContext.CurrentContext.Random.Next();
            var hasher = new PasswordHasher(format, Factory.CreateHashAlgorithm("SHA-256"));

            var password = TestContext.CurrentContext.Random.GetString();

            var salt = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(salt);

            var passwordHash = hasher.ComputeHash(password, salt);

            _passwordDb.Set(t.Handle, user.Guid, passwordHash, salt, format);

            var check = new PasswordCheck(t.Handle, _passwordDb, user.Origin.Id)
            {
                Algorithm = hasher
            };

            Assert.IsTrue(check.Check(user, password));

            var wrongPassword = TestContext.CurrentContext.Random.GetString();

            Assert.IsFalse(check.Check(user, wrongPassword));
        }
    }

    [Test]
    public void SinglePasswordCheckWithoutMatch()
    {
        using (var t = _engine.NewTransaction())
        {
            var format = TestContext.CurrentContext.Random.Next();
            var hasher = new PasswordHasher(format, Factory.CreateHashAlgorithm("SHA-256"));

            var user = CreateUser(t.Handle);
            var password = TestContext.CurrentContext.Random.GetString();

            var salt = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(salt);

            var passwordHash = hasher.ComputeHash(password, salt);

            _passwordDb.Set(t.Handle, user.Guid, passwordHash, salt, format);

            var originId = TestContext.CurrentContext.Random.Next();

            var check = new PasswordCheck(t.Handle, _passwordDb, originId)
            {
                Algorithm = hasher
            };

            Assert.IsFalse(check.Check(user, password));
        }
    }

    [Test]
    public void MultipleHashAlgorithms()
    {
        using (var t = _engine.NewTransaction())
        {
            var origin = Origin.Random();

            _userDb.WriteOrigin(t.Handle, origin);

            // first password format & user:
            var firstFormat = TestContext.CurrentContext.Random.Next();
            var firstHasher = new PasswordHasher(firstFormat, Factory.CreateHashAlgorithm("SHA-256"));

            var firstUser = User.Random(origin);

            _userDb.WriteUser(t.Handle, firstUser);

            var firstPassword = TestContext.CurrentContext.Random.GetString();

            var salt = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(salt);

            var hash = firstHasher.ComputeHash(firstPassword, salt);

            _passwordDb.Set(t.Handle, firstUser.Guid, hash, salt, firstFormat);

            // second password format & user:
            var secondFormat = TestContext.CurrentContext.Random.Next();
            var secondHasher = new PasswordHasher(secondFormat, Factory.CreateHashAlgorithm("SHA-512"));

            var secondUser = User.Random(origin);

            _userDb.WriteUser(t.Handle, secondUser);

            var secondPassword = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(salt);

            hash = secondHasher.ComputeHash(secondPassword, salt);

            _passwordDb.Set(t.Handle, secondUser.Guid, hash, salt, secondFormat);

            // test passwords:
            var check = new PasswordCheck(t.Handle, _passwordDb, origin.Id)
            {
                Algorithm = firstHasher.Concat(secondHasher)
            };

            Assert.IsTrue(check.Check(firstUser, firstPassword));
            Assert.IsFalse(check.Check(firstUser, secondPassword));
            Assert.IsTrue(check.Check(secondUser, secondPassword));
            Assert.IsFalse(check.Check(secondUser, firstPassword));
        }
    }

    [Test]
    public void MultiplePasswordChecks()
    {
        using (var t = _engine.NewTransaction())
        {
            // first check, hasher & user:
            var firstOrigin = Origin.Random();

            _userDb.WriteOrigin(t.Handle, firstOrigin);

            var firstFormat = TestContext.CurrentContext.Random.Next();
            var firstHasher = new PasswordHasher(firstFormat, Factory.CreateHashAlgorithm("SHA-256"));

            var firstUser = User.Random(firstOrigin);

            _userDb.WriteUser(t.Handle, firstUser);

            var firstPassword = TestContext.CurrentContext.Random.GetString();

            var salt = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(salt);

            var hash = firstHasher.ComputeHash(firstPassword, salt);

            _passwordDb.Set(t.Handle, firstUser.Guid, hash, salt, firstFormat);

            var firstCheck = new PasswordCheck(t.Handle, _passwordDb, firstOrigin.Id)
            {
                Algorithm = firstHasher
            };

            // second check, hasher & user:
            var secondOrigin = Origin.Random();

            _userDb.WriteOrigin(t.Handle, secondOrigin);

            var secondFormat = TestContext.CurrentContext.Random.Next();
            var secondHasher = new PasswordHasher(secondFormat, Factory.CreateHashAlgorithm("SHA-512"));

            var secondUser = User.Random(secondOrigin);

            _userDb.WriteUser(t.Handle, secondUser);

            var secondPassword = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(salt);

            hash = secondHasher.ComputeHash(secondPassword, salt);

            _passwordDb.Set(t.Handle, secondUser.Guid, hash, salt, secondFormat);

            var secondCheck = new PasswordCheck(t.Handle, _passwordDb, secondOrigin.Id)
            {
                Algorithm = secondHasher
            };

            // test passwords:
            var chain = firstCheck.Concat(secondCheck);

            Assert.IsTrue(chain.Check(firstUser, firstPassword));
            Assert.IsFalse(chain.Check(firstUser, secondPassword));
            Assert.IsTrue(chain.Check(secondUser, secondPassword));
            Assert.IsFalse(chain.Check(secondUser, firstPassword));
        }
    }

    [Test]
    public void WritePasswordWithPasswordChanger()
    {
        using (var t = _engine.NewTransaction())
        {
            var origin = Origin.Random();

            _userDb.WriteOrigin(t.Handle, origin);

            var format = TestContext.CurrentContext.Random.Next();
            var hasher = new PasswordHasher(format, Factory.CreateHashAlgorithm("SHA-256"));

            var user = User.Random(origin);

            _userDb.WriteUser(t.Handle, user);

            var password = TestContext.CurrentContext.Random.GetString();

            var changer = new PasswordChanger(t.Handle, _passwordDb, format, hasher);

            changer.Change(user, password);

            var storedPassword = _passwordDb.TryGet(t.Handle, user.Guid);

            Assert.NotNull(storedPassword);
            Assert.AreEqual(format, storedPassword!.Format);

            var hash = hasher.ComputeHash(password, storedPassword.Salt);

            Assert.IsTrue(hash.SequenceEqual(storedPassword.Hash));
        }
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract IUserDb CreateUserDb();

    protected abstract IPasswordDb CreatePasswordDb();

    IUser CreateUser(object handle)
    {
        var origin = Origin.Random();
        var user = User.Random(origin);

        _userDb.WriteOrigin(handle, origin);
        _userDb.WriteUser(handle, user);

        return user;
    }

    static (byte[], byte[], int) RandomPassword()
    {
        var hash = new byte[20];

        TestContext.CurrentContext.Random.NextBytes(hash);

        var salt = new byte[20];

        TestContext.CurrentContext.Random.NextBytes(salt);

        var format = TestContext.CurrentContext.Random.Next();

        return (hash, salt, format);
    }
}