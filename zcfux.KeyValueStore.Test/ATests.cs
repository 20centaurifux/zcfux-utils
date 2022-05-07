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

    protected abstract IStore CreateAndSetupStore();
}