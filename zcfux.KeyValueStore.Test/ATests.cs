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
using zcfux.Byte;

namespace zcfux.KeyValueStore.Test;

public abstract class ATests
{
    [Test]
    public void CreateAndSetup()
    {
        using (var store = CreateAndSetupStore())
        {
            Assert.IsInstanceOf<IStore>(store);
        }
    }

    [Test]
    public void PutAndFetch()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(key, value.ToMemoryStream());

            using (var stream = store.Fetch(key))
            {
                var ms = new MemoryStream();

                stream.CopyTo(ms);

                Assert.IsTrue(value.SequenceEqual(ms.ToArray()));
            }
        }
    }

    [Test]
    public void PutTwiceAndFetch()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            var first = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(first);

            store.Put(key, first.ToMemoryStream());

            var second = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(second);

            store.Put(key, second.ToMemoryStream());

            using (var stream = store.Fetch(key))
            {
                var ms = new MemoryStream();

                stream.CopyTo(ms);

                Assert.IsFalse(first.SequenceEqual(ms.ToArray()));
                Assert.IsTrue(second.SequenceEqual(ms.ToArray()));
            }
        }
    }

    [Test]
    public void FetchNonExisting()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            Assert.Throws<KeyNotFoundException>(() =>
            {
                store.Fetch(key);
            });
        }
    }

    [Test]
    public void Remove()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            var value = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(value);

            store.Put(key, value.ToMemoryStream());

            store.Remove(key);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                store.Fetch(key);
            });
        }
    }

    [Test]
    public void RemoveNonExisting()
    {
        using (var store = CreateAndSetupStore())
        {
            var key = TestContext.CurrentContext.Random.GetString();

            store.Remove(key);
        }
    }

    [Test]
    public void GetAllKeys()
    {
        using (var store = CreateAndSetupStore())
        {
            var firstKey = TestContext.CurrentContext.Random.GetString();

            var first = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(first);

            store.Put(firstKey, first.ToMemoryStream());

            var secondKey = TestContext.CurrentContext.Random.GetString();

            var second = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(second);

            store.Put(secondKey, second.ToMemoryStream());

            var fetchedKeys = store.GetKeys().ToArray();

            Assert.AreEqual(2, fetchedKeys.Length);
            Assert.IsTrue(fetchedKeys.Contains(firstKey));
            Assert.IsTrue(fetchedKeys.Contains(secondKey));
        }
    }

    [Test]
    public void QueryKeys()
    {
        using (var store = CreateAndSetupStore())
        {
            var first = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(first);

            store.Put("foo.bar", first.ToMemoryStream());

            var second = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(second);

            store.Put("foo.baz", second.ToMemoryStream());

            var third = new byte[20];

            TestContext.CurrentContext.Random.NextBytes(third);

            store.Put("_foo.ba$", third.ToMemoryStream());

            var fetchedKeys = store
                .GetKeys()
                .Where(key => key.StartsWith("foo."))
                .ToArray();

            Assert.AreEqual(2, fetchedKeys.Length);
            Assert.IsTrue(fetchedKeys.Contains("foo.bar"));
            Assert.IsTrue(fetchedKeys.Contains("foo.baz"));
        }
    }

    protected abstract IStore CreateAndSetupStore();
}