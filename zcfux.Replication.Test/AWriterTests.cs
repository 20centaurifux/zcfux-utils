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

namespace zcfux.Replication.Test
{
    public abstract class AWriterTests
    {
        [SetUp]
        public void Setup()
            => CreateDb();

        [TearDown]
        public void TearDown()
            => DropDb();

        protected abstract void CreateDb();

        protected abstract void DropDb();

        [Test]
        public void CreateWriter()
        {
            var writer = CreateWriter("a");

            Assert.IsInstanceOf<AWriter>(writer);
            Assert.AreEqual("a", writer.Side);
        }

        [Test]
        public void CreateEntity()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var timestamp = DateTime.UtcNow;

            var result = writer.TryCreate(model, timestamp);

            Assert.IsFalse(result.IsConflict);
            Assert.AreEqual(model.Guid, result.Version.Entity.Guid);
            Assert.AreEqual(model.Text, result.Version.Entity.Text);
            Assert.AreEqual(timestamp, result.Version.Modified);
            Assert.AreEqual("a", result.Version.Side);
            Assert.IsFalse(result.Version.IsNew);
            Assert.IsNotEmpty(result.Version.Revision);
        }

        [Test]
        public void CreateEntityTwice()
        {
            var a = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var timestamp = DateTime.UtcNow;

            writer.TryCreate(a, timestamp);

            var b = new Model
            {
                Guid = a.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var result = writer.TryCreate(b, timestamp);

            Assert.IsTrue(result.IsConflict);
            Assert.AreEqual(a.Guid, result.Version.Entity.Guid);
            Assert.AreEqual(a.Text, result.Version.Entity.Text);
            Assert.AreEqual(timestamp, result.Version.Modified);
            Assert.AreEqual("a", result.Version.Side);
            Assert.IsFalse(result.Version.IsNew);
            Assert.IsNotEmpty(result.Version.Revision);
        }

        [Test]
        public void UpdateEntity()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var now = DateTime.UtcNow;

            var first = writer.TryCreate(model, now.AddMinutes(-1)).Version;

            model.Text = TestContext.CurrentContext.Random.GetString();

            var second = writer.Update(
                model,
                first.Revision!,
                now,
                new Merge.LastWriteWins());

            Assert.AreEqual(model.Guid, second.Entity.Guid);
            Assert.AreEqual(model.Text, second.Entity.Text);
            Assert.AreEqual(now, second.Modified);
            Assert.AreEqual("a", second.Side);
            Assert.IsFalse(second.IsNew);
            Assert.IsNotEmpty(second.Revision);
            Assert.AreNotEqual(first.Revision, second.Revision);
        }

        [Test]
        public void UpdateEntityWithSameValue()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var now = DateTime.UtcNow;

            var first = writer.TryCreate(model, now.AddMinutes(-1)).Version;

            var second = writer.Update(
                model,
                first.Revision!,
                now,
                new Merge.LastWriteWins());

            Assert.AreEqual(first.IsNew, second.IsNew);
            Assert.AreEqual(first.Modified, second.Modified);
            Assert.AreEqual(first.Revision, second.Revision);
            Assert.AreEqual(first.Entity, second.Entity);
        }

        [Test]
        public void LocalGenericMerge()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var now = DateTime.UtcNow;

            var first = writer.TryCreate(model, now.AddMinutes(-2)).Version;

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var second = writer.Update(
                model,
                first.Revision!,
                now,
                new Merge.LastWriteWins());

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var third = writer.Update(
                model,
                first.Revision!,
                now.AddMinutes(-1),
                new Merge.LastWriteWins());

            Assert.AreEqual(third.IsNew, second.IsNew);
            Assert.AreEqual(third.Modified, second.Modified);
            Assert.AreEqual(third.Revision, second.Revision);
            Assert.AreEqual(third.Entity, second.Entity);
        }

        [Test]
        public void LocalNonGenericMerge()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter("a");

            var now = DateTime.UtcNow;

            var first = writer.TryCreate(model, now.AddMinutes(-2)).Version;

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var second = writer.Update(
                model,
                first.Revision!,
                now,
                new MergeModels());


            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var third = writer.Update(
                model,
                first.Revision!,
                now.AddMinutes(-1),
                new MergeModels());

            Assert.AreEqual(third.IsNew, second.IsNew);
            Assert.AreEqual(third.Modified, second.Modified);
            Assert.AreEqual(third.Revision, second.Revision);
            Assert.AreEqual(third.Entity, second.Entity);
        }

        protected abstract AWriter CreateWriter(string side);
    }
}