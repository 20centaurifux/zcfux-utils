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
using zcfux.Filter;

namespace zcfux.Audit.Test;

public abstract class ACatalogueTests : ADbTest
{
    Event.Utility? _utility;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _utility = new Event.Utility(_db!, _handle!);

        _utility.InsertDefaultEntries();
    }

    protected virtual void MoveEvents()
    {
    }

    protected abstract ECatalogue Catalogue { get; }

    [Test]
    public void QueryEvents_Id()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Id.EqualTo(ev.Id));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_Severity()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Low));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_CreatedAt()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow.AddMonths(-1), "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.CreatedAt.Between(ev.CreatedAt.AddDays(-1), ev.CreatedAt));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_KindId()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.KindId.EqualTo(ev.Kind.Id));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_Kind()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Kind.EqualTo(ev.Kind.Name));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_DisplayName()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.DisplayName.EqualTo("Login"));

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_WithSkip()
    {
        _utility!.InsertLoginEvent(
            DateTime.UtcNow.AddMonths(-1),
            "::1",
            "Alice",
            TestContext.CurrentContext.Random.GetString());

        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);

        MoveEvents();

        var qb = new QueryBuilder()
            .WithOrderBy(EventFilters.CreatedAt)
            .WithSkip(1);

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "127.0.0.1", "Bob", session);
    }

    [Test]
    public void QueryEvents_WithLimit()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow.AddMonths(-1), "::1", "Alice", session);

        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);

        MoveEvents();

        var qb = new QueryBuilder()
            .WithOrderBy(EventFilters.CreatedAt)
            .WithLimit(1);

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void QueryEvents_WithSkipAndLimit()
    {
        _utility!.InsertFailedLoginEvent(DateTime.UtcNow.AddMonths(-2), "127.0.0.1", "Rupert");

        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility.InsertLoginEvent(DateTime.UtcNow.AddMonths(-1), "::1", "Alice", session);

        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);

        MoveEvents();

        var qb = new QueryBuilder()
            .WithOrderBy(EventFilters.CreatedAt)
            .WithSkip(1)
            .WithLimit(1);

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session);
    }

    [Test]
    public void DeleteEvents_Id()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello");
        var second = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Bob", "world");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.Id.EqualTo(first.Id));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), second, "::1", "Bob", "world");
    }

    [Test]
    public void DeleteEvents_Severity()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.Severity.EqualTo(ESeverity.Critical));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world");
    }

    [Test]
    public void DeleteEvents_CreatedAt()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello");
        var second = _utility!.InsertLoginEvent(DateTime.UtcNow.AddDays(-1), "::1", "Bob", "world");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.CreatedAt.EqualTo(first.CreatedAt));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), second, "::1", "Bob", "world");
    }

    [Test]
    public void DeleteEvents_KindId()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.KindId.EqualTo(second.Kind.Id));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world");
    }

    [Test]
    public void DeleteEvents_Kind()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.Kind.EqualTo(second.Kind.Name));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world");
    }

    [Test]
    public void DeleteEvents_DisplayName()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.DisplayName.EqualTo(second.Topic!.DisplayName));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world");
    }

    [Test]
    public void DeleteEvents_OrphansDeleted()
    {
        var (ev, topic) = _utility!.InsertUserCreatedEvent(DateTime.UtcNow, "Alice");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.Id.EqualTo(ev.Id));

        var qb = new QueryBuilder()
            .WithFilter(TopicFilters.Id.EqualTo(topic.Id));

        var results = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(0, results.Length);
    }

    [Test]
    public void DeleteEvents_ReferencedTopicExists()
    {
        var alice = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.User, "Alice");

        var eventTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "User created");

        var firstEvent = _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Medium, DateTime.UtcNow, eventTopic);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.Created, alice);

        eventTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "User deleted");

        _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Medium, DateTime.UtcNow, eventTopic);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.Created, alice);

        MoveEvents();

        var catalogue = _db.Events.CreateCatalogue(_handle!, Catalogue);

        catalogue.Delete(_handle!, EventFilters.Id.EqualTo(firstEvent.Id));

        var qb = new QueryBuilder()
            .WithFilter(TopicFilters.Id.EqualTo(alice.Id));

        var topic = _db.Topics.QueryTopics(_handle!, qb.Build()).FirstOrDefault();

        Assert.IsInstanceOf<ITopic>(topic);

        Assert.AreEqual(alice.Id, topic!.Id);
        Assert.AreEqual(alice.Kind.Id, topic.Kind.Id);
        Assert.AreEqual(alice.Kind.Name, topic.Kind.Name);
        Assert.AreEqual(alice.DisplayName, topic.DisplayName);
    }

    void TestSingleLoginEventQuery(ICatalogue catalogue, Query query, IEvent ev, string endpoint, string username, string session)
    {
        var result = catalogue.QueryEvents(query).ToArray();

        Assert.AreEqual(1, result.Length);

        var e = result
            .First()
            .Item1;

        var edges = result
            .First()
            .Item2
            .ToArray();

        TestEquals(ev, e);

        Assert.AreEqual(3, edges.Count());

        TestContains(edges, Event.TopicKinds.Event, "Login", Event.Associations.ConnectedFrom, Event.TopicKinds.Endpoint, endpoint);
        TestContains(edges, Event.TopicKinds.Event, "Login", Event.Associations.AuthenticatedAs, Event.TopicKinds.User, username);
        TestContains(edges, Event.TopicKinds.User, username, Event.Associations.CreatedSession, Event.TopicKinds.Session, session);
    }

    [Test]
    public void QueryEvents_OrderById()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);
        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);
        _utility.InsertLoginEvent(DateTime.UtcNow, "::1", "Rupert", session);

        MoveEvents();

        TestOrderById(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderById(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Id);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(3, result.Length);

        var previous = long.MaxValue;

        foreach (var (ev, _) in result)
        {
            Assert.Less(ev.Id, previous);

            previous = ev.Id;
        }
    }

    [Test]
    public void QueryEvents_OrderBySeverity()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        TestOrderBySeverity(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderBySeverity(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Severity);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(ESeverity.Critical, result[0].Item1.Severity);
        Assert.AreEqual(ESeverity.Low, result[1].Item1.Severity);
    }

    [Test]
    public void QueryEvents_OrderByCreatedAt()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow.AddDays(-2), "::1", "Alice", session);
        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);
        _utility.InsertLoginEvent(DateTime.UtcNow.AddDays(-1), "::1", "Rupert", session);

        MoveEvents();

        TestOrderByCreatedAt(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderByCreatedAt(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderBy(EventFilters.CreatedAt);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(3, result.Length);

        var previous = DateTime.UnixEpoch;

        foreach (var (ev, _) in result)
        {
            Assert.Greater(ev.CreatedAt, previous);

            previous = ev.CreatedAt;
        }
    }

    [Test]
    public void QueryEvents_OrderByKindId()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        TestOrderByKindId(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderByKindId(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.KindId);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(Event.EventKinds.Service.Id, result[0].Item1.Kind.Id);
        Assert.AreEqual(Event.EventKinds.Security.Id, result[1].Item1.Kind.Id);
    }

    [Test]
    public void QueryEvents_OrderByKind()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        TestOrderByKind(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderByKind(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Kind);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(Event.EventKinds.Service.Name, result[0].Item1.Kind.Name);
        Assert.AreEqual(Event.EventKinds.Security.Name, result[1].Item1.Kind.Name);
    }

    [Test]
    public void QueryEvents_OrderByDisplayName()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        TestOrderByDisplayName(_db!.Events.CreateCatalogue(_handle!, Catalogue));
    }

    static void TestOrderByDisplayName(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.DisplayName);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual("Service status changed", result[0].Item1.Topic!.DisplayName);
        Assert.AreEqual("Login", result[1].Item1.Topic!.DisplayName);
    }

    [Test]
    public void FindAssociations_LeftTopicId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.LeftTopicId.EqualTo(ev.Topic!.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_LeftTopicKindId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_LeftTopicKind()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKind.EqualTo("Event"));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_LeftTopic()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.LeftTopic.EqualTo("User created"));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_AssociationId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.AssociationId.EqualTo(Event.Associations.Created.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_Association()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.Association.EqualTo("created"));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_RightTopicId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.RightTopicId.EqualTo(topic.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_RightTopicKindId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.RightTopicKindId.EqualTo(Event.TopicKinds.User.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_RightTopicKind()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.RightTopicKind.EqualTo("User"));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_RightTopic()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.RightTopic.EqualTo("Alice"));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_FilterEventId()
    {
        var ((ev, topic), _) = CreateAliceAndBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Id.EqualTo(ev.Id));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_FilterEventCreatedAt()
    {
        var ((ev1, topic1), (ev2, topic2)) = CreateAliceAndBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.CreatedAt.LessThanOrEqualTo(DateTime.UtcNow));

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        TestSingleEventWithSingleEdge(result.Where(r => r.Item1.Id == ev1.Id), ev1, Event.Associations.Created, topic1);

        TestSingleEventWithSingleEdge(result.Where(r => r.Item1.Id == ev2.Id), ev2, Event.Associations.Created, topic2);
    }

    [Test]
    public void FindAssociations_FilterEventSeverity()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.Severity.EqualTo(ESeverity.Critical));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Deleted, topic);
    }

    [Test]
    public void FindAssociations_FilterEventKindIdOrKind()
    {
        CreateAliceAndDeleteBob();

        var ev = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(Logical.Or(
                EventFilters.KindId.EqualTo(Event.EventKinds.Service.Id),
                EventFilters.Kind.EqualTo(Event.EventKinds.Service.Name)));

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(1, result.Length);

        TestEquals(ev, result[0].Item1);

        Assert.AreEqual(1, result[0].Item2.Count());

        var edge = result[0].Item2.First();

        Assert.AreEqual(ev.Topic!.Id, edge.Left.Id);
        Assert.AreEqual(ev.Topic.Kind.Id, edge.Left.Kind.Id);
        Assert.AreEqual(ev.Topic.Kind.Name, edge.Left.Kind.Name);
        Assert.AreEqual(ev.Topic.DisplayName, edge.Left.DisplayName);

        Assert.AreEqual(Event.Associations.Started.Id, edge.Association.Id);
        Assert.AreEqual(Event.Associations.Started.Name, edge.Association.Name);

        Assert.AreEqual(Event.TopicKinds.Service.Id, edge.Right.Kind.Id);
        Assert.AreEqual(Event.TopicKinds.Service.Name, edge.Right.Kind.Name);
        Assert.AreEqual("Mail", edge.Right.DisplayName);
    }

    [Test]
    public void FindAssociations_FilterDisplayName()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithFilter(EventFilters.DisplayName.EqualTo("User deleted"));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Deleted, topic);
    }

    [Test]
    public void FindAssociations_WithSkip()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithSkip(1)
            .WithOrderBy(EventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Deleted, topic);
    }

    [Test]
    public void FindAssociations_WithLimit()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithLimit(1)
            .WithOrderBy(EventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Created, topic);
    }

    [Test]
    public void FindAssociations_WithSkipAndLimit()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithSkip(1)
            .WithLimit(1)
            .WithOrderBy(EventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id));

        TestSingleEventWithSingleEdge(result, ev, Event.Associations.Deleted, topic);
    }

    [Test]
    public void FindAssociations_OrderByEventId()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Id);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Id, result[1].Item1.Id);
    }

    [Test]
    public void FindAssociations_OrderByCreatedAt()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.CreatedAt, result[1].Item1.CreatedAt);
    }

    [Test]
    public void FindAssociations_OrderBySeverity()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Severity);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Severity, result[1].Item1.Severity);
    }

    [Test]
    public void FindAssociations_OrderByKindId()
    {
        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", TestContext.CurrentContext.Random.GetString());
        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.KindId);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Kind.Id, result[1].Item1.Kind.Id);
    }

    [Test]
    public void FindAssociations_OrderByKind()
    {
        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");
        _utility.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", TestContext.CurrentContext.Random.GetString());

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.Kind);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Kind.Name, result[1].Item1.Kind.Name);
    }

    [Test]
    public void FindAssociations_OrderByDisplayName()
    {
        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");
        _utility.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", TestContext.CurrentContext.Random.GetString());

        MoveEvents();

        var catalogue = _db!.Events.CreateCatalogue(_handle!, Catalogue);

        var qb = new QueryBuilder()
            .WithOrderByDescending(EventFilters.DisplayName);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(Event.TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Topic!.DisplayName, result[1].Item1.Topic!.DisplayName);
    }

    ((IEvent, ITopic), (IEvent, ITopic)) CreateAliceAndDeleteBob()
    {
        var (ev1, topic1) = _utility!.InsertUserCreatedEvent(DateTime.UtcNow, "Alice");

        var (ev2, topic2) = _utility.InsertUserDeletedEvent(DateTime.UtcNow, "Bob");

        return ((ev1, topic1), (ev2, topic2));
    }

    ((IEvent, ITopic), (IEvent, ITopic)) CreateAliceAndBob()
    {
        var (ev1, topic1) = _utility!.InsertUserCreatedEvent(DateTime.UtcNow, "Alice");
        var (ev2, topic2) = _utility.InsertUserCreatedEvent(DateTime.UtcNow, "Bob");

        return ((ev1, topic1), (ev2, topic2));
    }

    static void TestSingleEventWithSingleEdge(
        IEnumerable<(IEvent, IEnumerable<IEdge>)> events,
        IEvent expectedEvent,
        IAssociation expectedAssociation,
        ITopic expectedRightTopic)
    {
        Assert.AreEqual(1, events.Count());

        var (ev, edges) = events.First();

        TestEquals(expectedEvent, ev);

        Assert.AreEqual(1, edges.Count());

        var edge = edges.First();

        Assert.AreEqual(expectedEvent.Topic!.Id, edge.Left.Id);
        Assert.AreEqual(expectedEvent.Topic.Kind.Id, edge.Left.Kind.Id);
        Assert.AreEqual(expectedEvent.Topic.Kind.Name, edge.Left.Kind.Name);
        Assert.AreEqual(expectedEvent.Topic.DisplayName, edge.Left.DisplayName);

        Assert.AreEqual(expectedAssociation.Id, edge.Association.Id);
        Assert.AreEqual(expectedAssociation.Name, edge.Association.Name);

        Assert.AreEqual(expectedRightTopic.Id, edge.Right.Id);
        Assert.AreEqual(expectedRightTopic.Kind.Id, edge.Right.Kind.Id);
        Assert.AreEqual(expectedRightTopic.Kind.Name, edge.Right.Kind.Name);
        Assert.AreEqual(expectedRightTopic.DisplayName, edge.Right.DisplayName);
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

    void TestContains(
        IEnumerable<IEdge> edges,
        ITopicKind leftKind,
        string leftDisplayName,
        IAssociation assoc,
        ITopicKind rightKind,
        string rightDisplayName)
    {
        var found = edges.Any(e => Event.Utility.Equals(e, leftKind, leftDisplayName, assoc, rightKind, rightDisplayName));

        Assert.IsTrue(found);
    }
}