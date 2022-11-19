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
using zcfux.Filter;
using zcfux.Security.Token;

namespace zcfux.Security.Test;

public abstract class ATokenTests
{
    IEngine _engine = null!;
    ITokenDb _db = null!;

    sealed record Kind(int Id, string Name)
        : IKind
    {
        public static Kind Random()
            => new(
                TestContext.CurrentContext.Random.Next(),
                TestContext.CurrentContext.Random.GetString());
    }

    sealed record Token(string Value, IKind Kind, int Counter, DateTime? EndOfLife)
        : IToken
    {
        public static Token Random(IKind kind)
            => new(
                TestContext.CurrentContext.Random.GetString(),
                kind,
                0,
                null);
    }

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _db = CreateTokenDb();
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract ITokenDb CreateTokenDb();

    [Test]
    public void WriteKind()
    {
        var kind = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);

            var qb = new QueryBuilder()
                .WithFilter(KindFilters.Id.EqualTo(kind.Id));

            var received = _db.QueryKinds(t.Handle, qb.Build()).Single();

            AssertKind(kind, received);
        }
    }

    [Test]
    public void OverWriteKind()
    {
        var first = Kind.Random();

        var second = new Kind(
            first.Id,
            TestContext.CurrentContext.Random.GetString());

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, first);
            _db.WriteKind(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(KindFilters.Id.EqualTo(first.Id));

            var received = _db.QueryKinds(t.Handle, qb.Build()).Single();

            AssertKind(second, received);
        }
    }

    [Test]
    public void OverWriteKindWithNameConflict()
    {
        var first = Kind.Random();

        var second = new Kind(
            TestContext.CurrentContext.Random.Next(),
            first.Name);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, first);

            Assert.Throws<ConflictException>(() => _db.WriteKind(t.Handle, second));
        }
    }

    [Test]
    public void DeleteKind()
    {
        var kind = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.DeleteKind(t.Handle, kind.Id);

            var qb = new QueryBuilder()
                .WithFilter(KindFilters.Id.EqualTo(kind.Id));

            var received = _db.QueryKinds(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteNonExistingKind()
    {
        var kind = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.DeleteKind(t.Handle, kind.Id);

            Assert.Throws<NotFoundException>(() => _db.DeleteKind(t.Handle, kind.Id));
        }
    }

    [Test]
    public void DeleteKindWithAssociatedToken()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            _db.DeleteKind(t.Handle, kind.Id);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Value.EqualTo(token.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void Query_Kind_Id()
    {
        var kind = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);

            var qb = new QueryBuilder()
                .WithFilter(KindFilters.Id.EqualTo(kind.Id));

            var received = _db.QueryKinds(t.Handle, qb.Build()).Single();

            AssertKind(kind, received);
        }
    }

    [Test]
    public void Query_Kind_Name()
    {
        var kind = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);

            var qb = new QueryBuilder()
                .WithFilter(KindFilters.Name.EqualTo(kind.Name));

            var received = _db.QueryKinds(t.Handle, qb.Build()).Single();

            AssertKind(kind, received);
        }
    }

    [Test]
    public void OrderBy_Kind_Id()
    {
        var first = Kind.Random();
        var second = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, first);
            _db.WriteKind(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(KindFilters.Id);

            var received = _db.QueryKinds(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);

            Assert.Less(received[0].Id, received[1].Id);
        }
    }

    [Test]
    public void OrderBy_Kind_Name()
    {
        var first = Kind.Random();
        var second = Kind.Random();

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, first);
            _db.WriteKind(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(KindFilters.Name);

            var received = _db.QueryKinds(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);

            Assert.Less(received[0].Name, received[1].Name);
        }
    }

    [Test]
    public void WriteToken()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Value.EqualTo(token.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void OverwriteToken()
    {
        var kind = Kind.Random();
        var first = Token.Random(kind);

        var second = new Token(
            first.Value,
            kind,
            TestContext.CurrentContext.Random.Next(),
            DateTime.UtcNow);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, first);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Value.EqualTo(second.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(second, received);
        }
    }

    [Test]
    public void WriteTokenWithNonExistingKind()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() => _db.WriteToken(t.Handle, token));
        }
    }

    [Test]
    public void DeleteToken()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);
            _db.DeleteToken(t.Handle, kind.Id, token.Value);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Value.EqualTo(token.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).SingleOrDefault();

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteNonExistingToken()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() => _db.DeleteToken(t.Handle, kind.Id, token.Value));
        }
    }

    [Test]
    public void DeleteTokens_Value()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var deleted = _db.DeleteTokens(t.Handle, TokenFilters.Value.EqualTo(token.Value));

            Assert.AreEqual(1, deleted);
            Assert.IsEmpty(_db.QueryTokens(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void DeleteTokens_KindId()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var deleted = _db.DeleteTokens(t.Handle, TokenFilters.KindId.EqualTo(kind.Id));

            Assert.AreEqual(1, deleted);
            Assert.IsEmpty(_db.QueryTokens(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void DeleteTokens_Kind()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var deleted = _db.DeleteTokens(t.Handle, TokenFilters.Kind.EqualTo(kind.Name));

            Assert.AreEqual(1, deleted);
            Assert.IsEmpty(_db.QueryTokens(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void DeleteTokens_Counter()
    {
        var kind = Kind.Random();

        var token = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            TestContext.CurrentContext.Random.Next(),
            null);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var deleted = _db.DeleteTokens(t.Handle, TokenFilters.Counter.EqualTo(token.Counter));

            Assert.AreEqual(1, deleted);
            Assert.IsEmpty(_db.QueryTokens(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void DeleteTokens_EndOfLife()
    {
        var kind = Kind.Random();

        var token = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            0,
            DateTime.Now);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var deleted = _db.DeleteTokens(t.Handle, TokenFilters.EndOfLife.EqualTo(token.EndOfLife!.Value));

            Assert.AreEqual(1, deleted);
            Assert.IsEmpty(_db.QueryTokens(t.Handle, QueryBuilder.All()));
        }
    }

    [Test]
    public void Query_Token_Value()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Value.EqualTo(token.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void Query_Token_KindId()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.KindId.EqualTo(kind.Id));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void Query_Token_Kind()
    {
        var kind = Kind.Random();
        var token = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Kind.EqualTo(kind.Name));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void Query_Token_Counter()
    {
        var kind = Kind.Random();

        var token = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            TestContext.CurrentContext.Random.Next(),
            null);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.Counter.EqualTo(token.Counter));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void Query_Token_EndOfLife()
    {
        var kind = Kind.Random();

        var token = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            0,
            DateTime.Now);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, token);

            var qb = new QueryBuilder()
                .WithFilter(TokenFilters.EndOfLife.EqualTo(token.EndOfLife!.Value));

            var received = _db.QueryTokens(t.Handle, qb.Build()).Single();

            AssertToken(token, received);
        }
    }

    [Test]
    public void OrderBy_Token_Value()
    {
        var kind = Kind.Random();
        var first = Token.Random(kind);
        var second = Token.Random(kind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, first);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(TokenFilters.Value);

            var received = _db.QueryTokens(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Value, received[1].Value);
        }
    }

    [Test]
    public void OrderBy_Token_KindId()
    {
        var firstKind = Kind.Random();
        var first = Token.Random(firstKind);

        var secondKind = Kind.Random();
        var second = Token.Random(secondKind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, firstKind);
            _db.WriteToken(t.Handle, first);

            _db.WriteKind(t.Handle, secondKind);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(TokenFilters.KindId);

            var received = _db.QueryTokens(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Kind.Id, received[1].Kind.Id);
        }
    }

    [Test]
    public void OrderBy_Token_Kind()
    {
        var firstKind = Kind.Random();
        var first = Token.Random(firstKind);

        var secondKind = Kind.Random();
        var second = Token.Random(secondKind);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, firstKind);
            _db.WriteToken(t.Handle, first);

            _db.WriteKind(t.Handle, secondKind);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(TokenFilters.Kind);

            var received = _db.QueryTokens(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Kind.Name, received[1].Kind.Name);
        }
    }

    [Test]
    public void OrderBy_Token_Counter()
    {
        var kind = Kind.Random();

        var first = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            TestContext.CurrentContext.Random.Next(),
            null);

        var second = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            TestContext.CurrentContext.Random.Next(),
            null);

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, first);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(TokenFilters.Counter);

            var received = _db.QueryTokens(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].Counter, received[1].Counter);
        }
    }

    [Test]
    public void OrderBy_Token_EndOfLife()
    {
        var kind = Kind.Random();

        var first = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            0,
            DateTime.Now);

        var second = new Token(
            TestContext.CurrentContext.Random.GetString(),
            kind,
            0,
            DateTime.Now.AddHours(1));

        using (var t = _engine.NewTransaction())
        {
            _db.WriteKind(t.Handle, kind);
            _db.WriteToken(t.Handle, first);
            _db.WriteToken(t.Handle, second);

            var qb = new QueryBuilder()
                .WithOrderBy(TokenFilters.EndOfLife);

            var received = _db.QueryTokens(t.Handle, qb.Build()).ToArray();

            Assert.AreEqual(2, received.Length);
            Assert.Less(received[0].EndOfLife, received[1].EndOfLife);
        }
    }

    static void AssertKind(IKind a, IKind b)
    {
        Assert.AreEqual(a.Id, b.Id);
        Assert.AreEqual(a.Name, b.Name);
    }

    static void AssertToken(IToken a, IToken b)
    {
        Assert.AreEqual(a.Value, b.Value);
        AssertKind(a.Kind, b.Kind);
        Assert.AreEqual(a.Counter, b.Counter);
        Assert.AreEqual(a.EndOfLife.HasValue, b.EndOfLife.HasValue);

        if (a.EndOfLife.HasValue && b.EndOfLife.HasValue)
        {
            Assert.AreEqual(0, (int)(a.EndOfLife.Value - b.EndOfLife.Value).TotalSeconds);
        }
    }
}