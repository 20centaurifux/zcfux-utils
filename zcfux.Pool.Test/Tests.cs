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

namespace zcfux.Pool.Test;

public sealed class Tests
{
    [Test]
    public void Create()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var r = pool.Create(uri);

        Assert.IsInstanceOf<A>(r);
        Assert.AreEqual(uri, r.Uri);
        Assert.AreEqual(1, r.Counter);
        Assert.IsFalse(r.Freed);
    }

    [Test]
    public void TakeFromEmptyPool()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var r = pool.TryTake(uri);

        Assert.IsNull(r);
    }

    [Test]
    public void TakeFromFilledPool()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var first = pool.Create(uri);

        first.Dispose();

        var second = pool.TryTake(uri);

        Assert.AreSame(first, second);

        Assert.IsInstanceOf<A>(first);
        Assert.AreEqual(uri, first.Uri);
        Assert.AreEqual(2, first.Counter);
        Assert.IsFalse(first.Freed);
    }

    [Test]
    public void TakeOrCreate_New()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var first = pool.TakeOrCreate(uri);

        Assert.IsInstanceOf<A>(first);
        Assert.AreEqual(uri, first.Uri);
        Assert.AreEqual(1, first.Counter);
        Assert.IsFalse(first.Freed);

        var second = pool.TakeOrCreate(uri);

        Assert.AreNotSame(first, second);

        Assert.IsInstanceOf<A>(second!);
        Assert.AreEqual(uri, second.Uri);
        Assert.AreEqual(1, second.Counter);
        Assert.IsFalse(second.Freed);
    }

    [Test]
    public void TakeOrCreate_Reuse()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var first = pool.TakeOrCreate(uri);

        Assert.IsInstanceOf<A>(first);
        Assert.AreEqual(uri, first.Uri);
        Assert.AreEqual(1, first.Counter);
        Assert.IsFalse(first.Freed);

        first.Dispose();

        var second = pool.TakeOrCreate(uri);

        Assert.AreSame(first, second);

        Assert.IsInstanceOf<A>(second!);
        Assert.AreEqual(uri, second.Uri);
        Assert.AreEqual(2, second.Counter);
        Assert.IsFalse(second.Freed);
    }

    [Test]
    public void LimitExceeded()
    {
        var pool = CreatePoolA();

        var uri = new Uri("test://");

        var first = pool.TakeOrCreate(uri);
        var second = pool.TakeOrCreate(uri);

        first.Dispose();
        second.Dispose();

        Assert.IsFalse(first.Freed);
        Assert.IsTrue(second.Freed);
    }

    static Pool<A> CreatePoolA()
        => new PoolBuilder<A>()
            .WithLimit(1)
            .WithFactory(uri => new A(uri))
            .Build();

    [Test]
    public void RecyclingFails()
    {
        var pool = new PoolBuilder<B>()
            .WithFactory(uri => new B(uri))
            .Build();

        var uri = new Uri("test://");

        var first = pool.Create(uri);

        first.Dispose();

        var second = pool.TryTake(uri);

        Assert.IsTrue(first.Freed);
        Assert.IsNull(second);
    }

    [Test]
    public void CustomKeyBuilder()
    {
        var pool = new PoolBuilder<A>()
            .WithFactory(uri => new A(uri))
            .WithKeyBuilder(uri => uri.Authority)
            .Build();

        var uri1 = new Uri("test://foo/a");

        var first = pool.Create(uri1);

        first.Dispose();

        var uri2 = new Uri("test://foo/b");

        var second = pool.TakeOrCreate(uri2);

        Assert.AreSame(first, second);

        Assert.IsInstanceOf<A>(second!);
        Assert.AreEqual(uri2, second.Uri);
        Assert.AreEqual(2, second.Counter);
        Assert.IsFalse(second.Freed);

        second.Dispose();

        var uri3 = new Uri("test://bar/c");

        var third = pool.TakeOrCreate(uri3);

        Assert.AreNotSame(first, third);

        Assert.IsInstanceOf<A>(third!);
        Assert.AreEqual(uri3, third.Uri);
        Assert.AreEqual(1, third.Counter);
        Assert.IsFalse(third.Freed);
    }

    [Test]
    public void DisposePool()
    {
        var pool = new PoolBuilder<A>()
            .WithFactory(uri => new A(uri))
            .Build();

        var first = pool.Create(new Uri("test://a"));

        first.Dispose();

        var second = pool.Create(new Uri("test://b"));

        second.Dispose();

        var third = pool.Create(new Uri("test://c"));

        pool.Dispose();

        Assert.IsTrue(first.Freed);
        Assert.IsTrue(second.Freed);
        Assert.IsFalse(third.Freed);
    }
}