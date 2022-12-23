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
using zcfux.Translation;

namespace zcfux.Audit.Test;

public abstract class ACatalogueTests : ADbTest
{
    Utility? _utility;
    Dictionary<string, ITranslation> _translations = default!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _utility = new Utility(_auditDb!, _translationDb!, _handle!);

        _utility.InsertDefaultEntries();

        _translations = new Dictionary<string, ITranslation>
        {
            ["de-DE"] = LoadTranslation("de-DE"),
            ["en-US"] = LoadTranslation("en-US")
        };
    }

    ITranslation LoadTranslation(string locale)
    {
        var translation = new Translation.Translation();

        var entries = _translationDb!.GetTranslation(_handle!, locale);

        translation.Setup(entries);

        return translation;
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
            .WithFilter(LocalizedEventFilters.Id.EqualTo(ev.Id));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void QueryEvents_Severity()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Low));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void QueryEvents_CreatedAt()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow.AddMonths(-1), "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.CreatedAt.Between(ev.CreatedAt.AddDays(-1), ev.CreatedAt));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void QueryEvents_KindId()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.KindId.EqualTo(ev.Kind.Id));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void QueryEvents_Kind()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Kind.EqualTo(ev.Kind.Name));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void QueryEvents_DisplayName()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.DisplayName.EqualTo("Login"));

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
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
            .WithOrderBy(LocalizedEventFilters.CreatedAt)
            .WithSkip(1);

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "127.0.0.1", "Bob", session, "en-US");
    }

    [Test]
    public void QueryEvents_WithLimit()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow.AddMonths(-1), "::1", "Alice", session);

        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);

        MoveEvents();

        var qb = new QueryBuilder()
            .WithOrderBy(LocalizedEventFilters.CreatedAt)
            .WithLimit(1);

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
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
            .WithOrderBy(LocalizedEventFilters.CreatedAt)
            .WithSkip(1)
            .WithLimit(1);

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, "en-US");
    }

    [Test]
    public void DeleteEvents_Id()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello");
        var second = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Bob", "world");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.Id.EqualTo(first.Id));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), second, "::1", "Bob", "world", "en-US");
    }

    [Test]
    public void DeleteEvents_Severity()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.Severity.EqualTo(ESeverity.Critical));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world", "en-US");
    }

    [Test]
    public void DeleteEvents_CreatedAt()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello");
        var second = _utility!.InsertLoginEvent(DateTime.UtcNow.AddDays(-1), "::1", "Bob", "world");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.CreatedAt.EqualTo(first.CreatedAt));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), second, "::1", "Bob", "world", "en-US");
    }

    [Test]
    public void DeleteEvents_KindId()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.KindId.EqualTo(second.Kind.Id));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world", "en-US");
    }

    [Test]
    public void DeleteEvents_Kind()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.Kind.EqualTo(second.Kind.Name));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world", "en-US");
    }

    [Test]
    public void DeleteEvents_DisplayName()
    {
        var first = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", "hello world");
        var second = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var displayName = _translations["en-US"].Translate(
            second.Topic!.DisplayName.Category.Name,
            second.Topic.DisplayName.MsgId)!;

        catalogue.Delete(LocalizedEventFilters.DisplayName.EqualTo(displayName));

        TestSingleLoginEventQuery(catalogue, QueryBuilder.All(), first, "::1", "Alice", "hello world", "en-US");
    }

    [Test]
    public void DeleteEvents_OrphansDeleted()
    {
        var (ev, topic) = _utility!.InsertUserCreatedEvent(DateTime.UtcNow, "Alice");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.Id.EqualTo(ev.Id));

        var qb = new QueryBuilder()
            .WithFilter(TopicFilters.Id.EqualTo(topic.Id));

        var results = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(0, results.Length);
    }

    [Test]
    public void DeleteEvents_ReferencedTopicRemains()
    {
        var alice = _auditDb!.Topics.NewTopic(
            _handle!,
            TopicKinds.User,
            _translationDb!.GetOrCreateTextResource(_handle!, _utility!.TextCategory, "Alice"));

        var eventTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Event,
            _translationDb!.GetOrCreateTextResource(_handle!, _utility!.TextCategory, "User created"));

        var firstEvent = _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Medium, DateTime.UtcNow, eventTopic);

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.Created, alice);

        eventTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Event,
            _translationDb!.GetOrCreateTextResource(_handle!, _utility!.TextCategory, "User deleted"));

        _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Medium, DateTime.UtcNow, eventTopic);

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.Created, alice);

        MoveEvents();

        var catalogue = _auditDb.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        catalogue.Delete(LocalizedEventFilters.Id.EqualTo(firstEvent.Id));

        var qb = new QueryBuilder()
            .WithFilter(TopicFilters.Id.EqualTo(alice.Id));

        var topic = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).FirstOrDefault();

        Assert.IsInstanceOf<ITopic>(topic);

        Assert.AreEqual(alice.Id, topic!.Id);
        Assert.AreEqual(alice.Kind.Id, topic.Kind.Id);
        Assert.AreEqual(alice.Kind.Name, topic.Kind.Name);
        Assert.AreEqual(alice.DisplayName.Id, topic.DisplayName.Id);
        Assert.AreEqual(alice.DisplayName.Category.Id, topic.DisplayName.Category.Id);
        Assert.AreEqual(alice.DisplayName.Category.Name, topic.DisplayName.Category.Name);
        Assert.AreEqual(alice.DisplayName.MsgId, topic.DisplayName.MsgId);
    }

    void TestSingleLoginEventQuery(
        ICatalogue catalogue,
        Query query,
        IEvent ev,
        string endpoint,
        string username,
        string session,
        string locale)
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

        TestEquals(ev, e, locale);

        Assert.AreEqual(3, edges.Count());

        TestContains(edges, TopicKinds.Event, "Login", Associations.ConnectedFrom, TopicKinds.Endpoint, endpoint);
        TestContains(edges, TopicKinds.Event, "Login", Associations.AuthenticatedAs, TopicKinds.User, username);
        TestContains(edges, TopicKinds.User, username, Associations.CreatedSession, TopicKinds.Session, session);
    }

    [Test]
    public void QueryEvents_OrderById()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);
        _utility.InsertLoginEvent(DateTime.UtcNow, "127.0.0.1", "Bob", session);
        _utility.InsertLoginEvent(DateTime.UtcNow, "::1", "Rupert", session);

        MoveEvents();

        TestOrderById(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderById(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Id);

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

        TestOrderBySeverity(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderBySeverity(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Severity);

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

        TestOrderByCreatedAt(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderByCreatedAt(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderBy(LocalizedEventFilters.CreatedAt);

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

        TestOrderByKindId(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderByKindId(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.KindId);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(EventKinds.Service.Id, result[0].Item1.Kind.Id);
        Assert.AreEqual(EventKinds.Security.Id, result[1].Item1.Kind.Id);
    }

    [Test]
    public void QueryEvents_OrderByKind()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        TestOrderByKind(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderByKind(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Kind);

        var result = catalogue.QueryEvents(qb.Build()).ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.AreEqual(EventKinds.Service.Name, result[0].Item1.Kind.Name);
        Assert.AreEqual(EventKinds.Security.Name, result[1].Item1.Kind.Name);
    }

    [Test]
    public void QueryEvents_OrderByDisplayName()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        TestOrderByDisplayName(_auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US"));
    }

    static void TestOrderByDisplayName(ICatalogue catalogue)
    {
        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.DisplayName);

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

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.LeftTopicId.EqualTo(ev.Topic!.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_LeftTopicKindId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_LeftTopicKind()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKind.EqualTo("Event"));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_LeftTopic()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.LeftTopic.EqualTo("User created"));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_AssociationId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.AssociationId.EqualTo(Associations.Created.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_Association()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.Association.EqualTo("created"));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_RightTopicId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.RightTopicId.EqualTo(topic.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_RightTopicKindId()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.RightTopicKindId.EqualTo(TopicKinds.User.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_RightTopicKind()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Medium));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.RightTopicKind.EqualTo("User"));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_RightTopic()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var result = catalogue.FindAssociations(
            QueryBuilder.All(),
            AssociationFilters.RightTopic.EqualTo("Alice"));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_FilterEventId()
    {
        var ((ev, topic), _) = CreateAliceAndBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Id.EqualTo(ev.Id));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_FilterEventCreatedAt()
    {
        var ((ev1, topic1), (ev2, topic2)) = CreateAliceAndBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.CreatedAt.LessThanOrEqualTo(DateTime.UtcNow));

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        TestSingleLocalizedEventWithSingleEdge(
            result.Where(r => r.Item1.Id == ev1.Id).ToArray(),
            ev1,
            Associations.Created,
            topic1,
            "en-US");

        TestSingleLocalizedEventWithSingleEdge(
            result.Where(r => r.Item1.Id == ev2.Id).ToArray(),
            ev2, Associations.Created,
            topic2,
            "en-US");
    }

    [Test]
    public void FindAssociations_FilterEventSeverity()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Severity.EqualTo(ESeverity.Critical));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Deleted, topic, "en-US");
    }

    [Test]
    public void FindAssociations_FilterEventKindIdOrKind()
    {
        CreateAliceAndDeleteBob();

        var ev = _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(Logical.Or(
                LocalizedEventFilters.KindId.EqualTo(EventKinds.Service.Id),
                LocalizedEventFilters.Kind.EqualTo(EventKinds.Service.Name)));

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(1, result.Length);

        TestEquals(ev, result[0].Item1, "en-US");

        Assert.AreEqual(1, result[0].Item2.Count());

        var edge = result[0].Item2.First();

        var displayName = _translations["en-US"].Translate(
            ev.Topic!.DisplayName.Category.Name,
            ev.Topic.DisplayName.MsgId);

        Assert.AreEqual(ev.Topic!.Id, edge.Left.Id);
        Assert.AreEqual(ev.Topic.Kind.Id, edge.Left.Kind.Id);
        Assert.AreEqual(ev.Topic.Kind.Name, edge.Left.Kind.Name);
        Assert.AreEqual(displayName, edge.Left.DisplayName);

        Assert.AreEqual(Associations.Started.Id, edge.Association.Id);
        Assert.AreEqual(Associations.Started.Name, edge.Association.Name);

        Assert.AreEqual(TopicKinds.Service.Id, edge.Right.Kind.Id);
        Assert.AreEqual(TopicKinds.Service.Name, edge.Right.Kind.Name);
        Assert.AreEqual("Mail", edge.Right.DisplayName);
    }

    [Test]
    public void FindAssociations_FilterDisplayName()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.DisplayName.EqualTo("User deleted"));

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Deleted, topic, "en-US");
    }

    [Test]
    public void FindAssociations_WithSkip()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithSkip(1)
            .WithOrderBy(LocalizedEventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Deleted, topic, "en-US");
    }

    [Test]
    public void FindAssociations_WithLimit()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithLimit(1)
            .WithOrderBy(LocalizedEventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, "en-US");
    }

    [Test]
    public void FindAssociations_WithSkipAndLimit()
    {
        var (_, (ev, topic)) = CreateAliceAndDeleteBob();

        _utility!.InsertServiceEvent(DateTime.UtcNow, started: true, serviceName: "Mail");

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithSkip(1)
            .WithLimit(1)
            .WithOrderBy(LocalizedEventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
            qb.Build(),
            AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id));

        TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Deleted, topic, "en-US");
    }

    [Test]
    public void FindAssociations_OrderByEventId()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Id);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.Id, result[1].Item1.Id);
    }

    [Test]
    public void FindAssociations_OrderByCreatedAt()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.CreatedAt);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
            .ToArray();

        Assert.AreEqual(2, result.Length);

        Assert.Greater(result[0].Item1.CreatedAt, result[1].Item1.CreatedAt);
    }

    [Test]
    public void FindAssociations_OrderBySeverity()
    {
        CreateAliceAndDeleteBob();

        MoveEvents();

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Severity);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
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

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.KindId);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
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

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.Kind);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
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

        var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, "en-US");

        var qb = new QueryBuilder()
            .WithOrderByDescending(LocalizedEventFilters.DisplayName);

        var result = catalogue.FindAssociations(
                qb.Build(),
                AssociationFilters.LeftTopicKindId.EqualTo(TopicKinds.Event.Id))
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

    void TestSingleLocalizedEventWithSingleEdge(
        (ILocalizedEvent, IEnumerable<ILocalizedEdge>)[] events,
        IEvent expectedEvent,
        IAssociation expectedAssociation,
        ITopic expectedRightTopic,
        string locale)
    {
        Assert.AreEqual(1, events.Length);

        var (ev, edges) = (events[0].Item1, events[0].Item2.ToArray());

        TestEquals(expectedEvent, ev, locale);

        Assert.AreEqual(1, edges.Length);

        var edge = edges[0];

        var expectedLeftTopicDisplayName = _translations[locale].Translate(
            expectedEvent.Topic!.DisplayName.Category.Name,
            expectedEvent.Topic.DisplayName.MsgId)!;

        Assert.AreEqual(expectedEvent.Topic!.Id, edge.Left.Id);
        Assert.AreEqual(expectedEvent.Topic.Kind.Id, edge.Left.Kind.Id);
        Assert.AreEqual(expectedEvent.Topic.Kind.Name, edge.Left.Kind.Name);
        Assert.AreEqual(expectedLeftTopicDisplayName, edge.Left.DisplayName);

        Assert.AreEqual(expectedAssociation.Id, edge.Association.Id);
        Assert.AreEqual(expectedAssociation.Name, edge.Association.Name);

        var expectedRightTopicDisplayName = _translations[locale].Translate(
                                                expectedRightTopic.DisplayName.Category.Name,
                                                expectedRightTopic.DisplayName.MsgId)
                                            ?? expectedRightTopic.DisplayName.MsgId;

        Assert.AreEqual(expectedRightTopic.Id, edge.Right.Id);
        Assert.AreEqual(expectedRightTopic.Kind.Id, edge.Right.Kind.Id);
        Assert.AreEqual(expectedRightTopic.Kind.Name, edge.Right.Kind.Name);
        Assert.AreEqual(expectedRightTopicDisplayName, edge.Right.DisplayName);
    }

    void TestEquals(IEvent first, ILocalizedEvent second, string locale)
    {
        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual(0, (int)(first.CreatedAt - second.CreatedAt).TotalSeconds);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.Severity, second.Severity);

        if (first.Topic is null)
        {
            Assert.IsNull(second.Topic);
        }
        else if (first.Topic.Translatable)
        {
            var translation = _translations[locale].Translate(
                first.Topic.DisplayName.Category.Name,
                first.Topic.DisplayName.MsgId);

            Assert.AreEqual(translation, second.Topic?.DisplayName);
        }
        else
        {
            Assert.AreEqual(first.Topic.DisplayName.MsgId, second.Topic?.DisplayName);
        }
    }

    void TestContains(
        IEnumerable<ILocalizedEdge> edges,
        ITopicKind leftKind,
        string leftDisplayName,
        IAssociation assoc,
        ITopicKind rightKind,
        string rightDisplayName)
    {
        var found = edges.Any(e => Utility.Equals(e, leftKind, leftDisplayName, assoc, rightKind, rightDisplayName));

        Assert.IsTrue(found);
    }
    
    [Test]
    public void QueryEvents_MultipleLanguages()
    {
        var session = TestContext.CurrentContext.Random.GetString();

        var ev = _utility!.InsertLoginEvent(DateTime.UtcNow, "::1", "Alice", session);

        _utility.InsertFailedLoginEvent(DateTime.UtcNow, "::1", "Bob");

        MoveEvents();

        var qb = new QueryBuilder()
            .WithFilter(LocalizedEventFilters.Id.EqualTo(ev.Id));

        foreach (var locale in new[] { "de-DE", "en-US" })
        {
            var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, locale);

            TestSingleLoginEventQuery(catalogue, qb.Build(), ev, "::1", "Alice", session, locale);
        }
    }
    
    [Test]
    public void FindAssociations_MultipleLanguages()
    {
        var ((ev, topic), _) = CreateAliceAndDeleteBob();

        MoveEvents();

        foreach (var locale in new[] { "de-DE", "en-US" })
        {
            var catalogue = _auditDb!.Events.CreateCatalogue(_handle!, Catalogue, locale);

            var result = catalogue.FindAssociations(
                QueryBuilder.All(),
                AssociationFilters.Association.EqualTo("created"));

            TestSingleLocalizedEventWithSingleEdge(result.ToArray(), ev, Associations.Created, topic, locale);
        }
    }
}