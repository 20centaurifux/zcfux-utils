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
using zcfux.Replication.Generic;
using zcfux.Replication.Merge;

namespace zcfux.Replication.Test
{
    public abstract class AResolverTests
    {
        protected const string Alice = "a";
        protected const string Bob = "b";

        [SetUp]
        public void Setup()
            => CreateDbs();

        [TearDown]
        public void TearDown()
            => DropDbs();

        protected abstract void CreateDbs();

        protected abstract void DropDbs();

        [Test]
        public void CreateResolver()
        {
            var resolver = CreateResolver(Alice);

            Assert.IsInstanceOf<AResolver>(resolver);
            Assert.AreEqual(Alice, resolver.Side);
        }

        [Test]
        public void ResolveConflictWithGenericMergeAlgorithm()
        {
            var (_, bob, conflict) = CreateConflict();

            var resolver = CreateResolver(Alice);

            resolver.Algorithms.Register<Model>(new LastWriteWins());

            resolver.Algorithms.Build();

            var resolved = resolver.Resolve(conflict);

            Assert.AreEqual(bob.Modified, resolved.Modified);
            Assert.AreEqual(bob.Revision, resolved.Revision);
            Assert.AreEqual(bob.Entity, resolved.Entity);

            var reader = CreateReader(Alice);

            var latest = reader.Read<Model>(resolved.Entity.Guid);

            Assert.AreEqual(latest.Revision, resolved.Revision);
        }


        [Test]
        public void ResolveConflictWithNonGenericMergeAlgorithm()
        {
            var (alice, _, conflict) = CreateConflict();

            var resolver = CreateResolver(Alice);

            resolver.Algorithms.Register(new FirstWrittenModelsWins());

            resolver.Algorithms.Build();

            var resolved = resolver.Resolve(conflict);

            Assert.AreEqual(alice.Modified, resolved.Modified);
            Assert.AreEqual(alice.Revision, resolved.Revision);
            Assert.AreEqual(alice.Entity, resolved.Entity);

            var reader = CreateReader(Alice);

            var latest = reader.Read<Model>(resolved.Entity.Guid);

            Assert.AreEqual(latest.Revision, resolved.Revision);
        }

        (IVersion<Model>, IVersion<Model>, IVersion) CreateConflict()
        {
            // Create & push initial version.
            var alice = CreateWriter(Alice);

            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            alice.TryCreate(model, DateTime.UtcNow.AddMinutes(-2), out var initialVersion);

            Push(Alice, Bob);

            // Update both local copies.
            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var alicesUpdate = alice.Update(model, initialVersion!.Revision!, DateTime.UtcNow.AddMinutes(-1), new LastWriteWins());

            var bob = CreateWriter(Bob);

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var bobsUpdate = bob.Update(model, initialVersion.Revision!, DateTime.UtcNow, new LastWriteWins());

            // Create & receive conflict.
            Push(Bob, Alice);

            var stream = CreateStreamReader(Alice);

            var source = new TaskCompletionSource<IVersion>();

            stream.Conflict += (s, e) => source.SetResult(e.Version);

            stream.Start();

            var success = source.Task.Wait(5000);

            Assert.IsTrue(success);

            stream.Stop();

            return (alicesUpdate, bobsUpdate, source.Task.Result);
        }

        protected abstract AWriter CreateWriter(string side);

        protected abstract AReader CreateReader(string side);

        protected abstract AStreamReader CreateStreamReader(string side);

        protected abstract AResolver CreateResolver(string side);

        protected abstract void Push(string from, string to);
    }
}