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
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using NUnit.Framework;
using zcfux.Replication.Merge;

namespace zcfux.Replication.Test
{
    public abstract class AStreamReaderTests
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
        public void CreateNewStreamReader()
        {
            var stream = CreateStreamReader(Alice);

            Assert.IsInstanceOf<AStreamReader>(stream);
            Assert.AreEqual(Alice, stream.Side);
        }

        [Test]
        public void StartStop()
        {
            var stream = CreateStreamReader(Alice);

            var startedSource = new TaskCompletionSource();

            stream.Started += (s, e) => startedSource.TrySetResult();

            var stoppedSource = new TaskCompletionSource();

            stream.Stopped += (s, e) => stoppedSource.TrySetResult();

            stream.Start();

            var started = startedSource.Task.Wait(5000);

            Assert.IsTrue(started);
            Assert.IsFalse(stoppedSource.Task.IsCompleted);

            stream.Stop();

            var stopped = startedSource.Task.Wait(5000);

            Assert.IsTrue(stopped);
        }

        [Test]
        public void ReceiveCreatedDocument()
        {
            var stream = CreateStreamReader(Alice);

            var readVersions = new ConcurrentBag<IVersion>();

            stream.Read += (s, e) => readVersions.Add(e.Version);

            var conflictedVersions = new ConcurrentBag<IVersion>();

            stream.Conflict += (s, e) => conflictedVersions.Add(e.Version);

            stream.Start();

            var writer = CreateWriter(Alice);

            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var result = writer.TryCreate(model, DateTime.UtcNow, out var createdVersion);

            Assert.AreEqual(ECreateResult.Success, result);

            WaitForEvents(1, readVersions);

            stream.Stop();

            var receivedVersion = readVersions.Single();

            Assert.AreEqual(createdVersion!.Revision, receivedVersion.Revision);
            Assert.AreEqual(createdVersion.Modified, receivedVersion.Modified);
            Assert.AreEqual(createdVersion.Side, receivedVersion.Side);
            Assert.AreEqual(createdVersion.Entity, receivedVersion.Entity);

            Assert.IsEmpty(conflictedVersions);
        }

        [Test]
        public void ReceiveUpdatedDocument()
        {
            var stream = CreateStreamReader(Alice);

            var readVersions = new ConcurrentBag<IVersion>();

            stream.Read += (s, e) => readVersions.Add(e.Version);

            var conflictedVersions = new ConcurrentBag<IVersion>();

            stream.Conflict += (s, e) => conflictedVersions.Add(e.Version);

            stream.Start();

            var writer = CreateWriter(Alice);

            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var result = writer.TryCreate(model, DateTime.UtcNow, out var createdVersion);

            Assert.AreEqual(ECreateResult.Success, result);

            WaitForEvents(1, readVersions);

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var updatedVersion = writer.Update(model, createdVersion!.Revision!, DateTime.UtcNow, new LastWriteWins());

            WaitForEvents(2, readVersions);

            stream.Stop();

            var receivedVersions = readVersions.ToArray();

            var receivedVersion = readVersions.Single(v => v.Revision == createdVersion.Revision);

            Assert.AreEqual(createdVersion.Revision, receivedVersion.Revision);
            Assert.AreEqual(createdVersion.Modified, receivedVersion.Modified);
            Assert.AreEqual(createdVersion.Side, receivedVersion.Side);
            Assert.AreEqual(createdVersion.Entity, receivedVersion.Entity);

            receivedVersion = readVersions.Single(v => v.Revision == updatedVersion.Revision);

            Assert.AreEqual(updatedVersion.Revision, receivedVersion.Revision);
            Assert.AreEqual(updatedVersion.Modified, receivedVersion.Modified);
            Assert.AreEqual(updatedVersion.Side, receivedVersion.Side);
            Assert.AreEqual(updatedVersion.Entity, receivedVersion.Entity);

            Assert.IsEmpty(conflictedVersions);
        }

        [Test]
        public void ReceiveConflict()
        {
            // Alice creates an initial version.
            var alice = CreateWriter(Alice);

            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var result = alice.TryCreate(model, DateTime.UtcNow.AddMinutes(-2), out var initialVersion);

            Assert.AreEqual(ECreateResult.Success, result);

            // Bob receives the version and updates his local copy.
            Push(Alice, Bob);

            var bob = CreateWriter(Bob);

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var bobsUpdate = bob.Update(model, initialVersion!.Revision!, DateTime.UtcNow, new LastWriteWins());

            // Alice updates her local copy.
            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var alicesUpdate = alice.Update(model, initialVersion.Revision!, DateTime.UtcNow.AddMinutes(-1), new LastWriteWins());

            // Now Alice receives Bob's version.
            Push(Bob, Alice);

            // The update stream has to contain the conflicted version.
            var stream = CreateStreamReader(Alice);

            var readVersions = new ConcurrentBag<IVersion>();

            stream.Read += (s, e) => readVersions.Add(e.Version);

            var conflictedVersions = new ConcurrentBag<IVersion>();

            stream.Conflict += (s, e) => conflictedVersions.Add(e.Version);

            stream.Start();

            WaitForEvents(1, conflictedVersions);

            stream.Stop();

            Assert.IsEmpty(readVersions);

            var conflictedVersion = conflictedVersions.Single();

            Assert.IsInstanceOf<IVersion>(conflictedVersion);
            Assert.AreEqual(initialVersion.Entity.Guid, conflictedVersion.Entity.Guid);
            Assert.Contains(conflictedVersion.Side, new string[] { Alice, Bob });

            var update = (conflictedVersion.Side == Alice)
                ? alicesUpdate
                : bobsUpdate;

            Assert.AreEqual(update.Modified, conflictedVersion.Modified);
            Assert.AreEqual(update.Revision, conflictedVersion.Revision);
            Assert.AreEqual(update.Entity, conflictedVersion.Entity);
        }

        [Test]
        public void ReceiveDeletedDocument()
        {
            var stream = CreateStreamReader(Alice);

            var readVersions = new ConcurrentBag<IVersion>();

            stream.Read += (s, e) => readVersions.Add(e.Version);

            var conflictedVersions = new ConcurrentBag<IVersion>();

            stream.Conflict += (s, e) => conflictedVersions.Add(e.Version);

            stream.Start();

            var writer = CreateWriter(Alice);

            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var result = writer.TryCreate(model, DateTime.UtcNow, out var createdVersion);

            Assert.AreEqual(ECreateResult.Success, result);

            WaitForEvents(1, readVersions);

            var deletedVersion = writer.Delete<Model>(model.Guid, DateTime.UtcNow);

            WaitForEvents(2, readVersions);

            stream.Stop();

            Assert.AreEqual(2, readVersions.Count());

            var receivedVersion = readVersions.Single(v => v.Revision == createdVersion!.Revision);

            Assert.AreEqual(createdVersion!.Modified, receivedVersion.Modified);
            Assert.AreEqual(createdVersion!.Side, receivedVersion.Side);
            Assert.AreEqual(createdVersion!.Entity, receivedVersion.Entity);
            Assert.IsFalse(createdVersion.IsDeleted);

            receivedVersion = readVersions.Single(v => v.Revision == deletedVersion.Revision);

            Assert.AreEqual(deletedVersion!.Modified, receivedVersion.Modified);
            Assert.AreEqual(deletedVersion!.Side, receivedVersion.Side);
            Assert.AreEqual(deletedVersion!.Entity, receivedVersion.Entity);
            Assert.IsTrue(deletedVersion.IsDeleted);

            Assert.IsEmpty(conflictedVersions);
        }

        void WaitForEvents(int expectedSum, params IEnumerable[] collections)
            => WaitForEvents(expectedSum, Stopwatch.StartNew(), collections);

        void WaitForEvents(int expectedSum, Stopwatch watch, params IEnumerable[] collections)
        {
            var sum = collections.Sum(coll => coll.Cast<object>().Count());

            if (sum < expectedSum)
            {
                Assert.Less(watch.ElapsedMilliseconds, 5000);

                Thread.Sleep(50);

                WaitForEvents(expectedSum, watch, collections);
            }
        }

        protected abstract AWriter CreateWriter(string side);

        protected abstract AStreamReader CreateStreamReader(string side);

        protected abstract void Push(string from, string to);
    }
}