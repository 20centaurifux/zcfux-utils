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
using zcfux.Audit.Test.Event;
using zcfux.Filter;

namespace zcfux.Audit.Test;

public abstract class AEventTests : ADbTest
{
    [Test]
    public void InsertAndGetEventKind()
    {
        var kind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, kind);

        var fetched = _db.Events.GetEventKind(_handle!, kind.Id);

        Assert.AreEqual(kind.Id, fetched.Id);
        Assert.AreEqual(kind.Name, fetched.Name);
    }

    [Test]
    public void InsertEventKindTwice()
    {
        var kind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, kind);

        Assert.That(() =>
        {
            _db.Events.InsertEventKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void InsertEventKindWithoutName()
    {
        var kind = new EventKind(TestContext.CurrentContext.Random.Next(), null!);

        Assert.That(() =>
        {
            _db!.Events.InsertEventKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void GetNonExistingEventKind()
    {
        Assert.That(() =>
        {
            _db!.Events.GetEventKind(_handle!, TestContext.CurrentContext.Random.Next());
        }, Throws.Exception);
    }

    [Test]
    public void NewEventWithTopic()
    {
        var eventKind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, eventKind);

        var topicKind = RandomTopicKind();

        _db.Topics.InsertTopicKind(_handle!, topicKind);

        var topic = _db.Topics.NewTopic(_handle!, topicKind, TestContext.CurrentContext.Random.GetString());

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            var ev = _db.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt, topic);

            Assert.Greater(ev.Id, 0);
            Assert.AreEqual(eventKind.Id, ev.Kind.Id);
            Assert.AreEqual(eventKind.Name, ev.Kind.Name);
            Assert.AreEqual(severity, ev.Severity);
            Assert.AreEqual(createdAt, ev.CreatedAt);
            Assert.IsFalse(ev.Archived);
            Assert.AreEqual(topic.Id, ev.Topic!.Id);
            Assert.AreEqual(topic.DisplayName, ev.Topic.DisplayName);
            Assert.AreEqual(topic.Kind.Id, ev.Topic.Kind.Id);
            Assert.AreEqual(topic.Kind.Name, ev.Topic.Kind.Name);
        }
    }

    [Test]
    public void NewEventWithNonExistingTopic()
    {
        var eventKind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, eventKind);

        var topic = new Topic
        {
            Id = TestContext.CurrentContext.Random.NextInt64(),
            Kind = RandomTopicKind(),
            DisplayName = TestContext.CurrentContext.Random.GetString()
        };

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() =>
            {
                _db.Events.NewEvent(_handle!, EventKinds.Security, (ESeverity)severity, createdAt, topic);
            }, Throws.Exception);
        }
    }

    [Test]
    public void NewEventWithoutTopic()
    {
        var eventKind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, eventKind);

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            var ev = _db.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt);

            Assert.Greater(ev.Id, 0);
            Assert.AreEqual(eventKind.Id, ev.Kind.Id);
            Assert.AreEqual(eventKind.Name, ev.Kind.Name);
            Assert.AreEqual(severity, ev.Severity);
            Assert.AreEqual(createdAt, ev.CreatedAt);
            Assert.IsFalse(ev.Archived);
            Assert.IsNull(ev.Topic);
        }
    }

    [Test]
    public void NewEventWithoutEventKind()
    {
        var eventKind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, eventKind);

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() =>
            {
                _db.Events.NewEvent(_handle!, null!, (ESeverity)severity, createdAt);
            }, Throws.Exception);
        }
    }

    [Test]
    public void NewEventWithNonExistingEventKind()
    {
        var eventKind = RandomEventKind();

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() =>
            {
                _db!.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt);
            }, Throws.Exception);
        }
    }

    [Test]
    public void ArchiveWithTopics()
    {
        var utility = new Utility(_db!, _handle!);

        utility.InsertDefaultEntries();

        var alice = utility.InsertLoginEvent(DateTime.UtcNow.AddDays(1), "::1", "Alice", "hello");
        var bob = utility.InsertLoginEvent(DateTime.UtcNow.AddDays(-1), "::1", "Bob", "world");

        _db!.Events.ArchiveEvents(_handle!, DateTime.UtcNow);

        var archivedEvents = _db.Events.CreateCatalogue(_handle!, ECatalogue.Archive);

        var qb = new QueryBuilder()
            .WithOrderBy(EventFilters.Id);

        var result = archivedEvents.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(1, result.Length);

        var (ev, edges) = result[0];

        TestEquals(bob, ev);

        Assert.IsTrue(ev.Archived);
        Assert.AreEqual(3, edges.Count());

        var recentEvents = _db.Events.CreateCatalogue(_handle!, ECatalogue.Recent);

        result = recentEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        (ev, edges) = result[0];

        TestEquals(alice, ev);

        Assert.IsFalse(ev.Archived);
        Assert.AreEqual(3, edges.Count());
    }

    [Test]
    public void ArchiveWithoutTopics()
    {
        var eventKind = RandomEventKind();

        _db!.Events.InsertEventKind(_handle!, eventKind);

        var old = _db.Events.NewEvent(_handle!, eventKind, ESeverity.Low, DateTime.UtcNow.AddDays(-1));
        var @new = _db.Events.NewEvent(_handle!, eventKind, ESeverity.Low, DateTime.UtcNow.AddDays(1));

        _db.Events.ArchiveEvents(_handle!, DateTime.UtcNow.AddSeconds(1));

        var archivedEvents = _db.Events.CreateCatalogue(_handle!, ECatalogue.Archive);

        var result = archivedEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        var (ev, edges) = result[0];

        TestEquals(old, ev);

        Assert.IsTrue(ev.Archived);
        Assert.AreEqual(0, edges.Count());

        var recentEvents = _db.Events.CreateCatalogue(_handle!, ECatalogue.Recent);

        result = recentEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        (ev, edges) = result[0];

        TestEquals(@new, ev);

        Assert.IsFalse(ev.Archived);
        Assert.AreEqual(0, edges.Count());
    }

    static void TestEquals(IEvent first, IEvent second)
    {
        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual(0, (int)(first.CreatedAt - second.CreatedAt).TotalSeconds);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.Severity, second.Severity);
        Assert.AreEqual(first.Topic?.Id, second.Topic?.Id);
        Assert.AreEqual(first.Topic?.DisplayName, second.Topic?.DisplayName);
    }

    static IEventKind RandomEventKind()
        => new EventKind(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());

    static ITopicKind RandomTopicKind()
        => new TopicKind(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());
}