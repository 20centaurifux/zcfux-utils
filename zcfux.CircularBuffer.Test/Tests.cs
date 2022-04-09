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

namespace zcfux.CircularBuffer.Test;

public sealed class Tests
{
    [Test]
    public void InvalidLimit()
    {
        Assert.Throws<ArgumentException>(() => new Buffer<int>(-1));
        Assert.Throws<ArgumentException>(() => new Buffer<int>(0));
    }

    [Test]
    public void Create()
    {
        var b = new Buffer<int>(1);

        Assert.IsFalse(b.IsFull);
        Assert.IsFalse(b.Any());
        Assert.AreEqual(0, b.Size);
        Assert.AreEqual(1, b.Capacity);
    }

    [Test]
    public void Append()
    {
        var b = new Buffer<int>(1);

        b.Append(1);

        Assert.AreEqual(1, b.Size);
        Assert.AreEqual(1, b.First());
    }

    [Test]
    public void IsFull()
    {
        var b = new Buffer<int>(1);

        b.Append(1);

        Assert.IsTrue(b.IsFull);
    }

    [Test]
    public void Clear()
    {
        var b = new Buffer<int>(1);

        b.Append(1);
        b.Clear();

        Assert.AreEqual(0, b.Size);
        Assert.IsFalse(b.Any());
    }

    [Test]
    public void Circulating()
    {
        var b = new Buffer<int>(2);

        b.Append(1);
        b.Append(2);
        b.Append(3);

        Assert.AreEqual(2, b.Size);

        var numbers = b.ToArray();

        Assert.AreEqual(2, numbers[0]);
        Assert.AreEqual(3, numbers[1]);
    }

    [Test]
    public void Pop()
    {
        var b = new Buffer<int>(2);

        b.Append(1);
        b.Append(2);
        b.Append(3);

        var popped = b.Pop();

        Assert.AreEqual(1, b.Size);
        Assert.AreEqual(3, popped);

        popped = b.Pop();

        Assert.AreEqual(0, b.Size);
        Assert.AreEqual(2, popped);

        Assert.Throws<InvalidOperationException>(() => b.Pop());
    }

    [Test]
    public void Dequeue()
    {
        var b = new Buffer<int>(2);

        b.Append(1);
        b.Append(2);
        b.Append(3);

        var dequeued = b.Dequeue();

        Assert.AreEqual(1, b.Size);
        Assert.AreEqual(2, dequeued);

        dequeued = b.Dequeue();

        Assert.AreEqual(0, b.Size);
        Assert.AreEqual(3, dequeued);

        Assert.Throws<InvalidOperationException>(() => b.Dequeue());
    }

    [Test]
    public void Contains()
    {
        var b = new Buffer<int>(2);

        b.Append(1);
        b.Append(2);
        b.Append(3);

        Assert.IsFalse(b.Contains(1));
        Assert.IsTrue(b.Contains(2));
        Assert.IsTrue(b.Contains(3));

        b.Pop();

        Assert.IsFalse(b.Contains(1));
        Assert.IsTrue(b.Contains(2));
        Assert.IsFalse(b.Contains(3));

        b.Pop();

        Assert.IsFalse(b.Contains(1));
        Assert.IsFalse(b.Contains(2));
        Assert.IsFalse(b.Contains(3));
    }
}