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
using zcfux.Translation.Data;

namespace zcfux.Audit.Test;

public abstract class ATopicTests : ADbTest
{
    readonly ICategory _textCategory = new TextCategory(1, "Test");

    public override void Setup()
    {
        base.Setup();

        _translationDb!.WriteCategory(_handle!, _textCategory);
    }

    [Test]
    public void InsertAndGetTopicKind()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var fetched = _auditDb.Topics.GetTopicKind(_handle!, kind.Id);

        Assert.AreEqual(kind.Id, fetched.Id);
        Assert.AreEqual(kind.Name, fetched.Name);
    }

    [Test]
    public void InsertTopicKindTwice()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        Assert.That(() =>
        {
            _auditDb.Topics.InsertTopicKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void InsertTopicKindWithoutName()
    {
        var kind = new TopicKind(TestContext.CurrentContext.Random.Next(), null!);

        Assert.That(() =>
        {
            _auditDb!.Topics.InsertTopicKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void GetNonExistingTopicKind()
    {
        Assert.That(() =>
        {
            _auditDb!.Topics.GetTopicKind(_handle!, TestContext.CurrentContext.Random.Next());
        }, Throws.Exception);
    }

    [Test]
    public void NewTopic()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = CreateRandomTextResource();

        var topic = _auditDb.Topics.NewTopic(_handle!, kind, displayName);

        Assert.Greater(topic.Id, 0);
        Assert.AreEqual(kind.Id, topic.Kind.Id);
        Assert.AreEqual(kind.Name, topic.Kind.Name);
        Assert.AreEqual(displayName.Id, topic.DisplayName.Id);
        Assert.AreEqual(displayName.Category.Id, topic.DisplayName.Category.Id);
        Assert.AreEqual(displayName.MsgId, topic.DisplayName.MsgId);
        Assert.IsFalse(topic.Translatable);
    }

    [Test]
    public void NewTranslatableTopic()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = CreateRandomTextResource();

        var topic = _auditDb.Topics.NewTranslatableTopic(_handle!, kind, displayName);

        Assert.Greater(topic.Id, 0);
        Assert.AreEqual(kind.Id, topic.Kind.Id);
        Assert.AreEqual(kind.Name, topic.Kind.Name);
        Assert.AreEqual(displayName.Id, topic.DisplayName.Id);
        Assert.AreEqual(displayName.Category.Id, topic.DisplayName.Category.Id);
        Assert.AreEqual(displayName.MsgId, topic.DisplayName.MsgId);
        Assert.IsTrue(topic.Translatable);
    }

    [Test]
    public void NewTopicWithoutKind()
    {
        var displayName = new TextResource(1, new TextCategory(1, "Test"), "Test");

        Assert.That(() =>
        {
            _auditDb!.Topics.NewTopic(_handle!, null!, displayName);
        }, Throws.Exception);
    }

    [Test]
    public void NewTranslatableTopicWithoutKind()
    {
        var displayName = new TextResource(1, new TextCategory(1, "Test"), "Test");

        Assert.That(() =>
        {
            _auditDb!.Topics.NewTranslatableTopic(_handle!, null!, displayName);
        }, Throws.Exception);
    }

    [Test]
    public void NewTopicWithNonExistingKind()
    {
        var kind = RandomTopicKind();

        var displayName = CreateRandomTextResource();

        Assert.That(() =>
        {
            _auditDb!.Topics.NewTopic(_handle!, kind, displayName);
        }, Throws.Exception);
    }

    [Test]
    public void NewTranslatableTopicWithNonExistingKind()
    {
        var kind = RandomTopicKind();

        var displayName = CreateRandomTextResource();

        Assert.That(() =>
        {
            _auditDb!.Topics.NewTranslatableTopic(_handle!, kind, displayName);
        }, Throws.Exception);
    }


    [Test]
    public void InsertSameTopicTwice()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = CreateRandomTextResource();

        var first = _auditDb.Topics.NewTopic(_handle!, kind, displayName);
        var second = _auditDb.Topics.NewTopic(_handle!, kind, displayName);

        Assert.AreNotEqual(first.Id, second.Id);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.DisplayName, second.DisplayName);
    }

    [Test]
    public void InsertSameTranslatableTopicTwice()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = CreateRandomTextResource();

        var first = _auditDb.Topics.NewTranslatableTopic(_handle!, kind, displayName);
        var second = _auditDb.Topics.NewTranslatableTopic(_handle!, kind, displayName);

        Assert.AreNotEqual(first.Id, second.Id);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.DisplayName, second.DisplayName);
    }

    [Test]
    public void AllTopics()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var topics = new ITopic[3];

        topics[0] = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        topics[1] = _auditDb.Topics.NewTranslatableTopic(_handle!, kind, CreateRandomTextResource());
        topics[2] = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.Id);

        var all = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(3, all.Length);

        foreach (var topic in topics)
        {
            var fetched = all.Single(t => t.Id == topic.Id);

            Assert.AreEqual(topic.Kind.Id, fetched.Kind.Id);
            Assert.AreEqual(topic.Kind.Name, fetched.Kind.Name);
            Assert.AreEqual(topic.DisplayName.Id, fetched.DisplayName.Id);
            Assert.AreEqual(topic.DisplayName.Category.Id, fetched.DisplayName.Category.Id);
            Assert.AreEqual(topic.DisplayName.Category.Name, fetched.DisplayName.Category.Name);
            Assert.AreEqual(topic.DisplayName.MsgId, fetched.DisplayName.MsgId);
            Assert.AreEqual(topic.Translatable, fetched.Translatable);
        }
    }

    [Test]
    public void AllTopicsWithLimit()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.Id);

        var first = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).First();

        qb = qb.WithLimit(1);

        var range = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, range.Id);
        Assert.AreEqual(first.Kind.Id, range.Kind.Id);
        Assert.AreEqual(first.Kind.Name, range.Kind.Name);
        Assert.AreEqual(first.DisplayName, range.DisplayName);
        Assert.AreEqual(first.Translatable, range.Translatable);
    }

    [Test]
    public void AllTopicsWithSkip()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.Id);

        var all = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        qb = qb.WithSkip(1);

        var range = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(2, range.Length);

        for (var i = 0; i < 2; i++)
        {
            Assert.AreEqual(all[i + 1].Id, range[i].Id);
            Assert.AreEqual(all[i + 1].Kind.Id, range[i].Kind.Id);
            Assert.AreEqual(all[i + 1].Kind.Name, range[i].Kind.Name);
            Assert.AreEqual(all[i + 1].DisplayName, range[i].DisplayName);
            Assert.AreEqual(all[i + 1].Translatable, range[i].Translatable);
        }
    }

    [Test]
    public void AllTopicsWithSkipAndLimit()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.Id);

        var last = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Last();

        qb = qb
            .WithSkip(2)
            .WithLimit(1);

        var range = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(last.Id, range.Id);
        Assert.AreEqual(last.Kind.Id, range.Kind.Id);
        Assert.AreEqual(last.Kind.Name, range.Kind.Name);
        Assert.AreEqual(last.DisplayName, range.DisplayName);
        Assert.AreEqual(last.Translatable, range.Translatable);
    }

    [Test]
    public void FilterId()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());
        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var qb = new QueryBuilder()
            .WithFilter(TopicFilters.Id.EqualTo(first.Id));

        var fetched = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, fetched.Id);
        Assert.AreEqual(first.Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(first.Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(first.DisplayName.Id, fetched.DisplayName.Id);
        Assert.AreEqual(first.DisplayName.Category.Id, fetched.DisplayName.Category.Id);
        Assert.AreEqual(first.DisplayName.Category.Name, fetched.DisplayName.Category.Name);
        Assert.AreEqual(first.DisplayName.MsgId, fetched.DisplayName.MsgId);
        Assert.AreEqual(first.Translatable, fetched.Translatable);
    }

    [Test]
    public void FilterTopicKind()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        kind = RandomTopicKind();

        _auditDb.Topics.InsertTopicKind(_handle!, kind);

        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var filter = Logical.And(
            TopicFilters.KindId.NotEqualTo(second.Kind.Id),
            TopicFilters.Kind.EqualTo(first.Kind.Name));

        var qb = new QueryBuilder()
            .WithFilter(filter);

        var fetched = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, fetched.Id);
        Assert.AreEqual(first.Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(first.Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(first.DisplayName.Id, fetched.DisplayName.Id);
        Assert.AreEqual(first.DisplayName.Category.Id, fetched.DisplayName.Category.Id);
        Assert.AreEqual(first.DisplayName.Category.Name, fetched.DisplayName.Category.Name);
        Assert.AreEqual(first.DisplayName.MsgId, fetched.DisplayName.MsgId);
        Assert.AreEqual(first.Translatable, fetched.Translatable);
    }

    [Test]
    public void FilterDisplayName()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        kind = RandomTopicKind();

        _auditDb.Topics.InsertTopicKind(_handle!, kind);

        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var filter = Logical.And(
            TopicFilters.TextId.EqualTo(first.DisplayName.Id),
            TopicFilters.TextCategoryId.EqualTo(first.DisplayName.Category.Id),
            TopicFilters.TextCategory.EqualTo(first.DisplayName.Category.Name),
            TopicFilters.MsgId.EqualTo(first.DisplayName.MsgId));

        var qb = new QueryBuilder()
            .WithFilter(filter);

        var fetched = _auditDb.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, fetched.Id);
        Assert.AreEqual(first.Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(first.Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(first.DisplayName.Id, fetched.DisplayName.Id);
        Assert.AreEqual(first.DisplayName.Category.Id, fetched.DisplayName.Category.Id);
        Assert.AreEqual(first.DisplayName.Category.Name, fetched.DisplayName.Category.Name);
        Assert.AreEqual(first.DisplayName.MsgId, fetched.DisplayName.MsgId);
        Assert.AreEqual(first.Translatable, fetched.Translatable);
    }

    ITextResource CreateRandomTextResource()
    {
        var msgid = TestContext.CurrentContext.Random.GetString();

        return _translationDb!.NewTextResource(_handle!, _textCategory, msgid);
    }

    static ITopicKind RandomTopicKind()
        => new TopicKind(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());
}