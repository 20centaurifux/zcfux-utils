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
using zcfux.Translation.Data;

namespace zcfux.Audit.Test;

public abstract class AEventTests : ADbTest
{
    readonly ICategory _textCategory = new TextCategory(1, "Test");

    public override void Setup()
    {
        base.Setup();

        _translationDb!.WriteCategory(_handle!, _textCategory);
    }

    [Test]
    public void InsertAndGetEventKind()
    {
        var kind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, kind);

        var fetched = _auditDb.Events.GetEventKind(_handle!, kind.Id);

        Assert.AreEqual(kind.Id, fetched.Id);
        Assert.AreEqual(kind.Name, fetched.Name);
    }

    [Test]
    public void InsertEventKindTwice()
    {
        var kind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, kind);

        Assert.That(() => { _auditDb.Events.InsertEventKind(_handle!, kind); }, Throws.Exception);
    }

    [Test]
    public void InsertEventKindWithoutName()
    {
        var kind = new EventKind(TestContext.CurrentContext.Random.Next(), null!);

        Assert.That(() => { _auditDb!.Events.InsertEventKind(_handle!, kind); }, Throws.Exception);
    }

    [Test]
    public void GetNonExistingEventKind()
    {
        Assert.That(() => { _auditDb!.Events.GetEventKind(_handle!, TestContext.CurrentContext.Random.Next()); }, Throws.Exception);
    }

    [Test]
    public void NewEventWithTopic()
    {
        var eventKind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, eventKind);

        var topicKind = RandomTopicKind();

        _auditDb.Topics.InsertTopicKind(_handle!, topicKind);

        var topic = _auditDb.Topics.NewTopic(_handle!, topicKind, CreateRandomTextResource());

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            var ev = _auditDb.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt, topic);

            Assert.Greater(ev.Id, 0);
            Assert.AreEqual(eventKind.Id, ev.Kind.Id);
            Assert.AreEqual(eventKind.Name, ev.Kind.Name);
            Assert.AreEqual(severity, ev.Severity);
            Assert.AreEqual(createdAt, ev.CreatedAt);
            Assert.IsFalse(ev.Archived);
            Assert.AreEqual(topic.Id, ev.Topic!.Id);
            Assert.AreEqual(topic.DisplayName.Id, ev.Topic.DisplayName.Id);
            Assert.AreEqual(topic.DisplayName.Category.Id, ev.Topic.DisplayName.Category.Id);
            Assert.AreEqual(topic.DisplayName.Category.Name, ev.Topic.DisplayName.Category.Name);
            Assert.AreEqual(topic.DisplayName.MsgId, ev.Topic.DisplayName.MsgId);
            Assert.AreEqual(topic.Translatable, ev.Topic.Translatable);
            Assert.AreEqual(topic.Kind.Id, ev.Topic.Kind.Id);
            Assert.AreEqual(topic.Kind.Name, ev.Topic.Kind.Name);
        }
    }

    [Test]
    public void NewEventWithNonExistingTopic()
    {
        var eventKind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, eventKind);

        var topic = new Topic
        {
            Id = TestContext.CurrentContext.Random.NextInt64(),
            Kind = RandomTopicKind(),
            DisplayName = CreateRandomTextResource()
        };

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() => { _auditDb.Events.NewEvent(_handle!, EventKinds.Security, (ESeverity)severity, createdAt, topic); },
                Throws.Exception);
        }
    }

    [Test]
    public void NewEventWithoutTopic()
    {
        var eventKind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, eventKind);

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            var ev = _auditDb.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt);

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

        _auditDb!.Events.InsertEventKind(_handle!, eventKind);

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() => { _auditDb.Events.NewEvent(_handle!, null!, (ESeverity)severity, createdAt); }, Throws.Exception);
        }
    }

    [Test]
    public void NewEventWithNonExistingEventKind()
    {
        var eventKind = RandomEventKind();

        var createdAt = DateTime.UtcNow;

        foreach (var severity in Enum.GetValues(typeof(ESeverity)))
        {
            Assert.That(() => { _auditDb!.Events.NewEvent(_handle!, eventKind, (ESeverity)severity, createdAt); }, Throws.Exception);
        }
    }

    [Test]
    public void ArchiveWithTopics()
    {
        var utility = new Utility(_auditDb!, _translationDb!, _handle!);

        utility.InsertDefaultEntries();

        var alice = utility.InsertLoginEvent(DateTime.UtcNow.AddDays(1), "::1", "Alice", "hello");
        var bob = utility.InsertLoginEvent(DateTime.UtcNow.AddDays(-1), "::1", "Bob", "world");

        _auditDb!.Events.ArchiveEvents(_handle!, DateTime.UtcNow);

        var archivedEvents = _auditDb.Events.CreateCatalogue(_handle!, ECatalogue.Archive, "en-US");

        var qb = new QueryBuilder()
            .WithOrderBy(LocalizedEventFilters.Id);

        var result = archivedEvents.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(1, result.Length);

        var (ev, edges) = result[0];
        
        Assert.AreEqual(bob.Id, ev.Id);
        Assert.AreEqual(0, (int)(bob.CreatedAt - ev.CreatedAt).TotalSeconds);
        Assert.AreEqual(bob.Kind.Id, ev.Kind.Id);
        Assert.AreEqual(bob.Kind.Name, ev.Kind.Name);
        Assert.AreEqual(bob.Severity, ev.Severity);
        Assert.AreEqual(bob.Topic?.Id, ev.Topic?.Id);
        Assert.AreEqual("Login", ev.Topic?.DisplayName);
        Assert.IsTrue(ev.Archived);
        Assert.AreEqual(3, edges.Count());

        var recentEvents = _auditDb.Events.CreateCatalogue(_handle!, ECatalogue.Recent, "en-US");

        result = recentEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        (ev, edges) = result[0];

        Assert.AreEqual(alice.Id, ev.Id);
        Assert.AreEqual(0, (int)(alice.CreatedAt - ev.CreatedAt).TotalSeconds);
        Assert.AreEqual(alice.Kind.Id, ev.Kind.Id);
        Assert.AreEqual(alice.Kind.Name, ev.Kind.Name);
        Assert.AreEqual(alice.Severity, ev.Severity);
        Assert.AreEqual(alice.Topic?.Id, ev.Topic?.Id);
        Assert.AreEqual("Login", ev.Topic?.DisplayName);
        Assert.IsFalse(ev.Archived);
        Assert.AreEqual(3, edges.Count());
    }

    [Test]
    public void ArchiveWithoutTopics()
    {
        var utility = new Utility(_auditDb!, _translationDb!, _handle!);

        utility.InsertDefaultEntries(); // creates required locale
        
        var eventKind = RandomEventKind();

        _auditDb!.Events.InsertEventKind(_handle!, eventKind);

        var old = _auditDb.Events.NewEvent(_handle!, eventKind, ESeverity.Low, DateTime.UtcNow.AddDays(-1));
        var @new = _auditDb.Events.NewEvent(_handle!, eventKind, ESeverity.Low, DateTime.UtcNow.AddDays(1));

        _auditDb.Events.ArchiveEvents(_handle!, DateTime.UtcNow.AddSeconds(1));

        var archivedEvents = _auditDb.Events.CreateCatalogue(_handle!, ECatalogue.Archive, "en-US");

        var result = archivedEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        var (ev, edges) = result[0];

        TestEquals(old, ev);

        Assert.IsTrue(ev.Archived);
        Assert.AreEqual(0, edges.Count());

        var recentEvents = _auditDb.Events.CreateCatalogue(_handle!, ECatalogue.Recent, "en-US");

        result = recentEvents.QueryEvents(QueryBuilder.All()).ToArray();

        Assert.AreEqual(1, result.Length);

        (ev, edges) = result[0];

        TestEquals(@new, ev);

        Assert.IsFalse(ev.Archived);
        Assert.AreEqual(0, edges.Count());
    }

    static void TestEquals(IEvent first, ILocalizedEvent second)
    {
        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual(0, (int)(first.CreatedAt - second.CreatedAt).TotalSeconds);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.Severity, second.Severity);
        Assert.AreEqual(first.Topic?.Id, second.Topic?.Id);
        Assert.AreEqual(first.Topic?.DisplayName.MsgId, second.Topic?.DisplayName);
    }

    ITextResource CreateRandomTextResource()
    {
        var msgid = TestContext.CurrentContext.Random.GetString();

        return _translationDb!.NewTextResource(_handle!, _textCategory, msgid);
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