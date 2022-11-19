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

public sealed class PersistentStoreDbTests : ATests
{
    protected override IStore CreateAndSetupStore()
    {
        SqliteConnection.ClearAllPools();

        var storagePath = BuildStoragePath();

        if (Directory.Exists(storagePath))
        {
            Directory.Delete(storagePath, recursive: true);
        }

        var opts = new Store.Options(storagePath, 512);

        var store = new Store(opts);

        store.Setup();

        return store;
    }

    [Test]
    public void Fsck_CheckDatabase()
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

        var connectionString = Db.BuildConnectionString(storagePath);

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                var bytes = new byte[20];

                TestContext.CurrentContext.Random.NextBytes(bytes);

                cmd.CommandText = "UPDATE Blob SET Contents=@blob WHERE Hash=@hash";

                cmd.Parameters.AddWithValue("@blob", bytes);
                cmd.Parameters.AddWithValue("@hash", firstHash);

                cmd.ExecuteNonQuery();
            }
        }

        // dry run:
        var fsck = new Fsck(storagePath)
        {
            Dry = true
        };

        var corrupted = new List<FsckEventArgs>();
        var missing = new List<FsckEventArgs>();
        var deleted = new List<FsckEventArgs>();

        fsck.Corrupted += (_, e) =>
        {
            Assert.AreEqual(ELocation.Database, e.Location);

            corrupted.Add(e);
        };

        fsck.Missing += (_, e) => missing.Add(e);

        fsck.Deleted += (_, e) =>
        {
            Assert.IsTrue(e.Location is ELocation.Index or ELocation.Database);

            deleted.Add(e);
        };

        fsck.CheckDatabase();

        Assert.AreEqual(1, corrupted.Count);
        Assert.AreEqual(firstHash, corrupted.First().Hash);

        Assert.AreEqual(0, missing.Count);

        Assert.AreEqual(0, deleted.Count);

        // delete corrupted blobs:
        corrupted.Clear();

        fsck.Dry = false;

        fsck.CheckDatabase();

        Assert.AreEqual(1, corrupted.Count);
        Assert.AreEqual(firstHash, corrupted.First().Hash);

        Assert.AreEqual(0, missing.Count);

        Assert.AreEqual(2, deleted.Count);
        Assert.AreEqual(firstHash, deleted.ElementAt(0).Hash);
        Assert.AreEqual(firstHash, deleted.ElementAt(1).Hash);
        Assert.IsTrue(deleted.ElementAt(0).Location != deleted.ElementAt(1).Location);
    }

    static string BuildStoragePath()
        => Path.Combine(Path.GetTempPath(), typeof(PersistentStoreDbTests).FullName!);
}