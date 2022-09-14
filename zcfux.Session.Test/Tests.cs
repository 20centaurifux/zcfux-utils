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
    Lesser General Public License for more detail

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using NUnit.Framework;

namespace zcfux.Session.Test;

public sealed class Tests
{
    Manager<MemoryStore> _manager = default!;

    [SetUp]
    public void Setup()
    {
        _manager = new Manager<MemoryStore>(TimeSpan.FromMilliseconds(500));

        _manager.Start();
    }

    [TearDown]
    public void TearDown()
        => _manager.Stop();

    [Test]
    public void NewSession()
    {
        var state = _manager.New();

        Assert.IsInstanceOf<SessionState>(state);
        Assert.IsInstanceOf<SessionId>(state.SessionId);
    }

    [Test]
    public void NewSessionIdsDiffer()
    {
        var a = _manager.New();
        var b = _manager.New();

        Assert.AreNotEqual(a.SessionId, b.SessionId);
    }

    [Test]
    public void GetSession()
    {
        var a = _manager.New();

        var b = _manager.Get(a.SessionId);

        Assert.AreSame(a, b);
    }

    [Test]
    public void GetNonExistingSession()
    {
        var id = SessionId.Random();

        Assert.Throws<SessionNotFoundException>(() => { _manager.Get(id); });
    }

    [Test]
    public void GetExpiredSession()
    {
        var state = _manager.New();

        Thread.Sleep(1000);

        Assert.Throws<SessionNotFoundException>(() => { _manager.Get(state.SessionId); });
    }

    [Test]
    public void RemoveSession()
    {
        var state = _manager.New();

        _manager.Remove(state.SessionId);

        Assert.Throws<SessionNotFoundException>(() => { _manager.Get(state.SessionId); });
    }

    [Test]
    public void RemoveNonExistingSession()
    {
        var id = SessionId.Random();

        _manager.Remove(id);
    }

    [Test]
    public void RemoveExpiredSession()
    {
        var state = _manager.New();

        Thread.Sleep(1000);

        _manager.Remove(state.SessionId);
    }

    [Test]
    public void KeepAlive()
    {
        var state = _manager.New();

        for (var i = 0; i < 8; ++i)
        {
            state.KeepAlive();

            Thread.Sleep(250);
        }

        Thread.Sleep(1000);

        Assert.Throws<SessionNotFoundException>(() => { _manager.Get(state.SessionId); });
    }

    [Test]
    public void Events()
    {
        var created = new HashSet<SessionId>();
        var expired = new HashSet<SessionId>();
        var removed = new HashSet<SessionId>();

        _manager.Created += (s, e) => { created.Add(e.SessionId); };

        _manager.Expired += (s, e) => { expired.Add(e.State.SessionId); };

        _manager.Removed += (s, e) => { removed.Add(e.SessionId); };

        var first = _manager.New();

        _manager.Remove(first.SessionId);

        var second = _manager.New();

        Thread.Sleep(1000);

        Assert.AreEqual(2, created.Count);

        Assert.IsTrue(created.Contains(first.SessionId));
        Assert.IsTrue(created.Contains(second.SessionId));

        Assert.AreEqual(1, expired.Count);

        Assert.IsTrue(created.Contains(second.SessionId));

        Assert.AreEqual(2, removed.Count);

        Assert.IsTrue(created.Contains(first.SessionId));
        Assert.IsTrue(created.Contains(second.SessionId));
    }

    [Test]
    public void ReadWriteValue()
    {
        var a = _manager.New();

        a["id"] = TestContext.CurrentContext.Random.GetString();
        a["value"] = TestContext.CurrentContext.Random.Next();

        var b = _manager.Get(a.SessionId);

        Assert.IsTrue(a.Has("id"));
        Assert.IsTrue(a.Has("value"));

        Assert.IsTrue(b.Has("id"));
        Assert.IsTrue(b.Has("value"));

        Assert.AreEqual(a["id"], b["id"]);
        Assert.AreEqual(a["value"], b["value"]);

        b.Remove("id");

        b["value"] = TestContext.CurrentContext.Random.Next();

        var c = _manager.Get(b.SessionId);

        Assert.IsFalse(a.Has("id"));
        Assert.IsFalse(b.Has("id"));
        Assert.IsFalse(c.Has("id"));

        Assert.AreEqual(b["value"], c["value"]);
    }

    [Test]
    public void ReadWriteValueWhenExpired()
    {
        var state = _manager.New();

        Thread.Sleep(1000);

        Assert.Throws<SessionNotFoundException>(() => { state["id"] = TestContext.CurrentContext.Random.Next(); });

        Assert.Throws<SessionNotFoundException>(() => { state.Has("id"); });

        Assert.Throws<SessionNotFoundException>(() => { state.Remove("id"); });

        Assert.Throws<SessionNotFoundException>(() => { state.KeepAlive(); });
    }
}