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
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using zcfux.Byte;
using zcfux.KeyValueStore.Persistent;

namespace zcfux.KeyValueStore.Test;

public sealed class PersistentStoreFsTests : ATests
{
    protected override IStore CreateAndSetupStore()
    {
        SqliteConnection.ClearAllPools();

        var buildStoragePath = BuildStoragePath();

        if (Directory.Exists(buildStoragePath))
        {
            Directory.Delete(buildStoragePath, recursive: true);
        }

        var opts = new Store.Options(buildStoragePath, 1);

        var store = new Store(opts);

        store.Setup();

        return store;
    }

    [Test]
    public void DeleteOrphans()
    {
        using (var store = CreateAndSetupStore())
        {
            var first = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            var firstHash = SHA256.HashData(value).ToHex();

            store.Put(first, value.ToMemoryStream());

            var second = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(second, value.ToMemoryStream());

            store.Remove(second);

            (store as Store)!.CollectGarbage();

            var blobsPath = Path.Combine(BuildStoragePath(), "blobs");

            var blobs = new BlobDirectory(blobsPath)
                .FindAll()
                .ToArray();

            Assert.AreEqual(1, blobs.Length);
            Assert.AreEqual(firstHash, blobs[0].Item1);
        }
    }

    [Test]
    public void DontDeleteOrphanWithReadLock()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            var hash = SHA256.HashData(value).ToHex();

            store.Put(key, value.ToMemoryStream());

            var stream = store.Fetch(key); // acquires read lock

            store.Remove(key);

            // write lock can not be acquired if the read lock is held by same thread
            Task.Run(() => (store as Store)!.CollectGarbage()).Wait();

            var blobsPath = Path.Combine(BuildStoragePath(), "blobs");

            var (receivedHash, _) = new BlobDirectory(blobsPath)
                .FindAll()
                .Single();

            Assert.AreEqual(hash, receivedHash);

            stream.Dispose(); // releases read lock

            (store as Store)!.CollectGarbage();

            var blobs = new BlobDirectory(blobsPath)
                .FindAll()
                .ToArray();

            Assert.AreEqual(0, blobs.Length);
        }
    }

    [Test]
    public void Fsck_CheckFilesystem()
    {
        string firstHash;

        // insert two test blobs:
        using (var store = CreateAndSetupStore())
        {
            var first = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            firstHash = SHA256.HashData(value).ToHex();

            store.Put(first, value.ToMemoryStream());

            var second = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(second, value.ToMemoryStream());
        }

        // overwrite first blob's contents:
        var storagePath = BuildStoragePath();

        var blobPath = PathBuilder.BuildBlobPath(storagePath, firstHash);

        var newValue = new byte[20];

        TestContext.CurrentContext.Random.NextBytes(newValue);

        File.WriteAllBytes(blobPath, newValue);

        // dry run:
        var fsck = new Fsck(storagePath)
        {
            Dry = true
        };

        var corrupted = new List<FsckEventArgs>();
        var deleted = new List<FsckEventArgs>();
        var missing = new List<FsckEventArgs>();

        fsck.Corrupted += (_, e) =>
        {
            Assert.AreEqual(ELocation.Filesystem, e.Location);

            corrupted.Add(e);
        };

        fsck.Missing += (_, e) => missing.Add(e);

        fsck.Deleted += (_, e) =>
        {
            Assert.IsTrue(e.Location is ELocation.Index or ELocation.Filesystem);

            deleted.Add(e);
        };

        fsck.CheckFilesystem();

        Assert.AreEqual(1, corrupted.Count);
        Assert.AreEqual(firstHash, corrupted.First().Hash);

        Assert.AreEqual(0, missing.Count);

        Assert.AreEqual(0, deleted.Count);

        // delete corrupted blobs:
        corrupted.Clear();

        fsck.Dry = false;

        fsck.CheckFilesystem();

        Assert.AreEqual(1, corrupted.Count);
        Assert.AreEqual(firstHash, corrupted.First().Hash);

        Assert.AreEqual(0, missing.Count);

        Assert.AreEqual(2, deleted.Count);
        Assert.AreEqual(firstHash, deleted.ElementAt(0).Hash);
        Assert.AreEqual(firstHash, deleted.ElementAt(1).Hash);
        Assert.IsTrue(deleted.ElementAt(0).Location != deleted.ElementAt(1).Location);
    }

    [Test]
    public void Fsck_CheckIndex()
    {
        string firstHash;

        // insert two test blobs:
        using (var store = CreateAndSetupStore())
        {
            var first = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            firstHash = SHA256.HashData(value).ToHex();

            store.Put(first, value.ToMemoryStream());

            var second = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(second, value.ToMemoryStream());
        }

        // overwrite first blob:
        var storagePath = BuildStoragePath();

        var blobPath = PathBuilder.BuildBlobPath(storagePath, firstHash);

        File.Delete(blobPath);

        // dry run:
        var fsck = new Fsck(storagePath)
        {
            Dry = true
        };

        var corrupted = new List<string>();
        var deleted = new List<FsckEventArgs>();
        var missing = new List<FsckEventArgs>();

        fsck.Corrupted += (_, e) => corrupted.Add(e.Hash);

        fsck.Missing += (_, e) =>
        {
            Assert.IsTrue(e.Location == ELocation.Filesystem);

            missing.Add(e);
        };

        fsck.Deleted += (_, e) =>
        {
            Assert.IsTrue(e.Location == ELocation.Index);

            deleted.Add(e);
        };

        fsck.CheckIndex();

        Assert.AreEqual(0, corrupted.Count);

        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual(firstHash, missing.First().Hash);

        Assert.AreEqual(0, deleted.Count);

        // delete missing blobs from index:
        missing.Clear();

        fsck.Dry = false;

        fsck.CheckIndex();

        Assert.AreEqual(0, corrupted.Count);

        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual(firstHash, missing.First().Hash);

        Assert.AreEqual(1, deleted.Count);
        Assert.AreEqual(firstHash, deleted.First().Hash);
    }

    static string BuildStoragePath()
        => Path.Combine(Path.GetTempPath(), typeof(PersistentStoreFsTests).FullName!);
}