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

        var path = BuildPath();

        Directory.Delete(path, recursive: true);

        var opts = new Store.Options(path, 1);

        var store = new Store(opts);

        store.Setup();

        return store;
    }

    string BuildPath()
        => Path.Combine(Path.GetTempPath(), typeof(PersistentStoreFsTests).FullName!);

    [Test]
    public void DeleteOrphans()
    {
        using (var store = CreateAndSetupStore())
        {
            var first = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            var firstHash = Hash(value);

            store.Put(first, value.ToMemoryStream());

            var second = TestContext.CurrentContext.Random.GetString();

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(second, value.ToMemoryStream());

            store.Remove(second);

            (store as Store)!.CollectGarbage();

            var blobsPath = Path.Combine(BuildPath(), "blobs");

            var blobs = new BlobsFromFs(blobsPath)
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

            var hash = Hash(value);

            store.Put(key, value.ToMemoryStream());

            var stream = store.Fetch(key); // acquires read lock

            store.Remove(key);

            // write lock can not be acquired if the read lock is held by same thread
            Task.Factory.StartNew(() => (store as Store)!.CollectGarbage()).Wait();

            var blobsPath = Path.Combine(BuildPath(), "blobs");

            var (receivedHash, _) = new BlobsFromFs(blobsPath)
                .FindAll()
                .Single();

            Assert.AreEqual(hash, receivedHash);

            stream.Dispose(); // releases read lock

            (store as Store)!.CollectGarbage();

            var blobs = new BlobsFromFs(blobsPath)
                .FindAll()
                .ToArray();

            Assert.AreEqual(0, blobs.Length);
        }
    }

    static string Hash(byte[] bytes)
    {
        var sha256 = SHA256.Create();

        var hash = sha256.ComputeHash(bytes);

        return hash.ToHex();
    }
}