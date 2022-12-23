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
using zcfux.Audit.LinqToPg;
using zcfux.Translation.Data;

namespace zcfux.Audit.Test;

public abstract class AAssociationTests : ADbTest
{
    readonly ICategory _textCategory = new TextCategory(1, "Test");

    public override void Setup()
    {
        base.Setup();

        _translationDb!.WriteCategory(_handle!, _textCategory);
    }

    [Test]
    public void InsertAndGetAssociation()
    {
        var association = RandomAssociation();

        _auditDb!.Associations.InsertAssociation(_handle!, association);

        var fetched = _auditDb.Associations.GetAssociation(_handle!, association.Id);

        Assert.AreEqual(association.Id, fetched.Id);
        Assert.AreEqual(association.Name, fetched.Name);
    }

    [Test]
    public void InsertSameAssociationTwice()
    {
        var association = RandomAssociation();

        _auditDb!.Associations.InsertAssociation(_handle!, association);

        Assert.That(() =>
        {
            _auditDb.Associations.InsertAssociation(_handle!, association);
        }, Throws.Exception);
    }

    [Test]
    public void InsertAssociationWithoutName()
    {
        var association = new Association(
            TestContext.CurrentContext.Random.Next(),
            null!);

        Assert.That(() =>
        {
            _auditDb!.Associations.InsertAssociation(_handle!, association);
        }, Throws.Exception);
    }

    [Test]
    public void GetNonExistingAssociation()
    {
        Assert.That(() =>
        {
            _auditDb!.Associations.GetAssociation(_handle!, TestContext.CurrentContext.Random.Next());
        }, Throws.Exception);
    }

    [Test]
    public void Associate()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var association = RandomAssociation();

        _auditDb.Associations.InsertAssociation(_handle!, association);

        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var edge = _auditDb.Associations.Associate(_handle!, first, association, second);

        Assert.AreEqual(first.Id, edge.Left.Id);
        Assert.AreEqual(first.Kind.Id, edge.Left.Kind.Id);
        Assert.AreEqual(first.Kind.Name, edge.Left.Kind.Name);
        Assert.AreEqual(first.DisplayName, edge.Left.DisplayName);

        Assert.AreEqual(association.Id, edge.Association.Id);
        Assert.AreEqual(association.Name, edge.Association.Name);

        Assert.AreEqual(second.Id, edge.Right.Id);
        Assert.AreEqual(second.Kind.Id, edge.Right.Kind.Id);
        Assert.AreEqual(second.Kind.Name, edge.Right.Kind.Name);
        Assert.AreEqual(second.DisplayName, edge.Right.DisplayName);
    }

    [Test]
    public void AssociateWithNonExistingTopic()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var association = RandomAssociation();

        _auditDb.Associations.InsertAssociation(_handle!, association);

        var second = new Topic
        {
            Id = TestContext.CurrentContext.Random.Next(),
            DisplayName = CreateRandomTextResource(),
            Kind = kind
        };

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, first, association, second);
        }, Throws.Exception);

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, second, association, first);
        }, Throws.Exception);
    }

    [Test]
    public void AssociateWithoutTopic()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var topic = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var association = RandomAssociation();

        _auditDb.Associations.InsertAssociation(_handle!, association);

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, topic, association, null!);
        }, Throws.Exception);

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, null!, association, topic);
        }, Throws.Exception);
    }

    [Test]
    public void AssociateWithNonExistingAssociation()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var association = RandomAssociation();

        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, first, association, second);
        }, Throws.Exception);
    }

    [Test]
    public void AssociateWithoutAssociation()
    {
        var kind = RandomTopicKind();

        _auditDb!.Topics.InsertTopicKind(_handle!, kind);

        var first = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        var second = _auditDb.Topics.NewTopic(_handle!, kind, CreateRandomTextResource());

        Assert.That(() =>
        {
            _auditDb.Associations.Associate(_handle!, first, null!, second);
        }, Throws.Exception);
    }

    ITextResource CreateRandomTextResource()
    {
        var msgid = TestContext.CurrentContext.Random.GetString();

        return _translationDb!.NewTextResource(_handle!, _textCategory, msgid);
    }

    static IAssociation RandomAssociation()
        => new Association(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());

    static ITopicKind RandomTopicKind()
        => new TopicKind(
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString());
}