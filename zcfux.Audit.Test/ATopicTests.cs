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

public abstract class ATopicTests : ADbTest
{
    [Test]
    public void InsertAndGetTopicKind()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var fetched = _db.Topics.GetTopicKind(_handle!, kind.Id);

        Assert.AreEqual(kind.Id, fetched.Id);
        Assert.AreEqual(kind.Name, fetched.Name);
    }

    [Test]
    public void InsertTopicKindTwice()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        Assert.That(() =>
        {
            _db.Topics.InsertTopicKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void InsertTopicKindWithoutName()
    {
        var kind = new TopicKind(TestContext.CurrentContext.Random.Next(), null!);

        Assert.That(() =>
        {
            _db!.Topics.InsertTopicKind(_handle!, kind);
        }, Throws.Exception);
    }

    [Test]
    public void GetNonExistingTopicKind()
    {
        Assert.That(() =>
        {
            _db!.Topics.GetTopicKind(_handle!, TestContext.CurrentContext.Random.Next());
        }, Throws.Exception);
    }

    [Test]
    public void NewTopic()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = TestContext.CurrentContext.Random.GetString();

        var topic = _db.Topics.NewTopic(_handle!, kind, displayName);

        Assert.Greater(topic.Id, 0);
        Assert.AreEqual(kind.Id, topic.Kind.Id);
        Assert.AreEqual(kind.Name, topic.Kind.Name);
        Assert.AreEqual(displayName, topic.DisplayName);
    }

    [Test]
    public void NewTopicWithoutKind()
    {
        var displayName = TestContext.CurrentContext.Random.GetString();

        Assert.That(() =>
        {
            _db!.Topics.NewTopic(_handle!, null!, displayName);
        }, Throws.Exception);
    }

    [Test]
    public void NewTopicWithNonExistingKind()
    {
        var kind = RandomTopicKind();

        var displayName = TestContext.CurrentContext.Random.GetString();

        Assert.That(() =>
        {
            _db!.Topics.NewTopic(_handle!, kind, displayName);
        }, Throws.Exception);
    }

    [Test]
    public void NewTopicWithoutDisplayName()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        Assert.That(() =>
        {
            _db.Topics.NewTopic(_handle!, kind, null!);
        }, Throws.Exception);
    }

    [Test]
    public void InsertSameTopicTwice()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var displayName = TestContext.CurrentContext.Random.GetString();

        var first = _db.Topics.NewTopic(_handle!, kind, displayName);
        var second = _db.Topics.NewTopic(_handle!, kind, displayName);

        Assert.AreNotEqual(first.Id, second.Id);
        Assert.AreEqual(first.Kind.Id, second.Kind.Id);
        Assert.AreEqual(first.Kind.Name, second.Kind.Name);
        Assert.AreEqual(first.DisplayName, second.DisplayName);
    }

    [Test]
    public void AllTopics()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var topics = new ITopic[3];

        topics[0] = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        topics[1] = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        topics[2] = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.DisplayName);

        var all = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(3, all.Length);

        foreach (var topic in topics)
        {
            var fetched = all.Single(t => t.Id == topic.Id);

            Assert.AreEqual(topic.Kind.Id, fetched.Kind.Id);
            Assert.AreEqual(topic.Kind.Name, fetched.Kind.Name);
            Assert.AreEqual(topic.DisplayName, fetched.DisplayName);
        }
    }

    [Test]
    public void AllTopicsWithLimit()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.DisplayName);

        var first = _db.Topics.QueryTopics(_handle!, qb.Build()).First();

        qb = qb.WithLimit(1);

        var range = _db.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, range.Id);
        Assert.AreEqual(first.Kind.Id, range.Kind.Id);
        Assert.AreEqual(first.Kind.Name, range.Kind.Name);
        Assert.AreEqual(first.DisplayName, range.DisplayName);
    }

    [Test]
    public void AllTopicsWithSkip()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.DisplayName);

        var all = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        qb = qb.WithSkip(1);

        var range = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(2, range.Length);

        for (var i = 0; i < 2; i++)
        {
            Assert.AreEqual(all[i + 1].Id, range[i].Id);
            Assert.AreEqual(all[i + 1].Kind.Id, range[i].Kind.Id);
            Assert.AreEqual(all[i + 1].Kind.Name, range[i].Kind.Name);
            Assert.AreEqual(all[i + 1].DisplayName, range[i].DisplayName);
        }
    }

    [Test]
    public void AllTopicsWithSkipAndLimit()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.DisplayName);

        var last = _db.Topics.QueryTopics(_handle!, qb.Build()).Last();

        qb = qb
            .WithSkip(2)
            .WithLimit(1);

        var range = _db.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(last.Id, range.Id);
        Assert.AreEqual(last.Kind.Id, range.Kind.Id);
        Assert.AreEqual(last.Kind.Name, range.Kind.Name);
        Assert.AreEqual(last.DisplayName, range.DisplayName);
    }

    [Test]
    public void FilterNameAndId()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var first = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());
        var second = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var filter = Logical.And(
            TopicFilters.Id.EqualTo(first.Id),
            TopicFilters.DisplayName.NotEqualTo(second.DisplayName));

        var qb = new QueryBuilder()
            .WithFilter(filter)
            .WithOrderBy(TopicFilters.DisplayName);

        var fetched = _db.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, fetched.Id);
        Assert.AreEqual(first.Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(first.Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(first.DisplayName, fetched.DisplayName);
    }

    [Test]
    public void FilterTopicKind()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var first = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        kind = RandomTopicKind();

        _db.Topics.InsertTopicKind(_handle!, kind);

        var second = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var filter = Logical.And(
            TopicFilters.KindId.NotEqualTo(second.Kind.Id),
            TopicFilters.Kind.EqualTo(first.Kind.Name));

        var qb = new QueryBuilder()
            .WithFilter(filter)
            .WithOrderBy(TopicFilters.DisplayName);

        var fetched = _db.Topics.QueryTopics(_handle!, qb.Build()).Single();

        Assert.AreEqual(first.Id, fetched.Id);
        Assert.AreEqual(first.Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(first.Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(first.DisplayName, fetched.DisplayName);
    }

    [Test]
    public void FilterWithRange()
    {
        var kind = RandomTopicKind();

        _db!.Topics.InsertTopicKind(_handle!, kind);

        var first = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        kind = RandomTopicKind();

        _db.Topics.InsertTopicKind(_handle!, kind);

        var second = _db.Topics.NewTopic(_handle!, kind, TestContext.CurrentContext.Random.GetString());

        var qb = new QueryBuilder()
            .WithOrderBy(TopicFilters.DisplayName);

        var all = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        var filter = Logical.Or(
            TopicFilters.KindId.EqualTo(first.Kind.Id),
            TopicFilters.KindId.EqualTo(second.Kind.Id));

        qb = qb
            .WithFilter(filter)
            .WithSkip(1);

        var range = _db.Topics.QueryTopics(_handle!, qb.Build()).ToArray();

        Assert.AreEqual(1, range.Length);

        var fetched = range.First();

        Assert.AreEqual(all.Last().Id, fetched.Id);
        Assert.AreEqual(all.Last().Kind.Id, fetched.Kind.Id);
        Assert.AreEqual(all.Last().Kind.Name, fetched.Kind.Name);
        Assert.AreEqual(all.Last().DisplayName, fetched.DisplayName);
    }

    ITopicKind RandomTopicKind()
        => new TopicKind(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());
}