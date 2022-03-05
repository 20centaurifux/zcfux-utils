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
using static System.Int32;

namespace zcfux.Pool.Test;

public sealed class PoolBuilderTests
{
    [Test]
    public void BuildWithoutFactory()
    {
        var builder = new PoolBuilder<A>();

        Assert.Throws<NullReferenceException>(() => builder.Build());
    }

    [Test]
    public void BuildWithAnonymousFactory()
    {
        var builder = new PoolBuilder<A>();

        var called = false;

        var pool = builder
            .WithFactory(uri =>
            {
                called = true;

                return new A(uri);
            })
            .Build();

        Assert.IsInstanceOf<Pool<A>>(pool);

        var a = pool.Create(new Uri("test://"));

        Assert.IsInstanceOf<A>(a);
        Assert.IsTrue(called);
    }

    [Test]
    public void BuildWithFactory()
    {
        var builder = new PoolBuilder<A>();

        var factory = new Factory();

        var pool = builder
            .WithFactory(factory)
            .Build();

        Assert.IsInstanceOf<Pool<A>>(pool);

        var a = pool.Create(new Uri("test://"));

        Assert.IsInstanceOf<A>(a);
        Assert.AreEqual(1, factory.Counter);
    }

    [Test]
    public void BuildWithAnonymousKeyBuilder()
    {
        var builder = new PoolBuilder<A>();

        var called = false;

        var pool = builder
            .WithFactory(new Factory())
            .WithKeyBuilder(uri =>

            {
                called = true;

                return uri.AbsolutePath;
            })
            .Build();

        Assert.IsInstanceOf<Pool<A>>(pool);

        var a = pool.Create(new Uri("test://"));

        Assert.IsInstanceOf<A>(a);
        Assert.IsFalse(called);

        a.Dispose();

        Assert.IsTrue(called);
    }

    [Test]
    public void BuildWithKeyBuilder()
    {
        var builder = new PoolBuilder<A>();

        var keyBuilder = new KeyBuilder();

        var pool = builder
            .WithFactory(new Factory())
            .WithKeyBuilder(keyBuilder)
            .Build();

        Assert.IsInstanceOf<Pool<A>>(pool);

        var a = pool.Create(new Uri("test://"));

        Assert.IsInstanceOf<A>(a);
        Assert.AreEqual(0, keyBuilder.Counter);

        a.Dispose();

        Assert.AreEqual(1, keyBuilder.Counter);
    }

    [Test]
    public void BuildWithLimit()
    {
        var builder = new PoolBuilder<A>();

        var limit = TestContext.CurrentContext.Random.Next(1, MaxValue);

        var pool = builder
            .WithFactory(new Factory())
            .WithLimit(limit)
            .Build();

        Assert.IsInstanceOf<Pool<A>>(pool);
        Assert.AreEqual(limit, pool.Limit);
    }
}