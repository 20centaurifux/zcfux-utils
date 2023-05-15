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

namespace zcfux.PriorityQueue.Test;

public sealed class Tests
{
    [Test]
    public void Enqueue_Pop()
    {
        var q = new PriorityQueue<int>();

        q.Enqueue(7);
        q.Enqueue(3);

        Assert.AreEqual(7, q.Pop());
        Assert.AreEqual(3, q.Pop());
    }
    
    [Test]
    public void Enqueue_TryPop()
    {
        var q = new PriorityQueue<int>();

        q.Enqueue(7);
        q.Enqueue(3);

        var success = q.TryPop(out var value);
        
        Assert.IsTrue(success);
        Assert.AreEqual(7, value);
        
        success = q.TryPop(out value);
        
        Assert.IsTrue(success);
        Assert.AreEqual(3, value);
    }
    
    [Test]
    public void Enqueue_Pop_Count()
    {
        var q = new PriorityQueue<int>();

        Assert.AreEqual(0, q.Count);
        
        q.Enqueue(11);
        
        Assert.AreEqual(1, q.Count);
        
        q.Pop();

        Assert.AreEqual(0, q.Count);
    }
    
    [Test]
    public void Enqueue_TryPop_Count()
    {
        var q = new PriorityQueue<int>();

        Assert.AreEqual(0, q.Count);
        
        q.Enqueue(11);
        
        Assert.AreEqual(1, q.Count);
        
        q.TryPop(out _);
        
        Assert.AreEqual(0, q.Count);
    }
    
    [Test]
    public void Pop_Empty()
    {
        var q = new PriorityQueue<int>();

        Assert.Throws<InvalidOperationException>(() => q.Pop());
    }
    
    [Test]
    public void TryPop_Empty()
    {
        var q = new PriorityQueue<int>();

        Assert.IsFalse(q.TryPop(out _));
    }
    
    [Test]
    public void Enqueue_Peek()
    {
        var q = new PriorityQueue<int>();

        q.Enqueue(7);
        q.Enqueue(3);

        Assert.AreEqual(7, q.Peek());
    }
    
    [Test]
    public void Enqueue_TryPeek()
    {
        var q = new PriorityQueue<int>();

        q.Enqueue(7);
        q.Enqueue(3);

        var success = q.TryPeek(out var value);
        
        Assert.IsTrue(success);
        Assert.AreEqual(7, value);
    }

    [Test]
    public void Enqueue_Peek_Count()
    {
        var q = new PriorityQueue<int>();

        Assert.AreEqual(0, q.Count);
        
        q.Enqueue(11);
        
        Assert.AreEqual(1, q.Count);
        
        q.Peek();

        Assert.AreEqual(1, q.Count);
    }
    
    [Test]
    public void Enqueue_TryPeek_Count()
    {
        var q = new PriorityQueue<int>();

        Assert.AreEqual(0, q.Count);
        
        q.Enqueue(11);
        
        Assert.AreEqual(1, q.Count);
        
        q.TryPeek(out _);

        Assert.AreEqual(1, q.Count);
    }
    
    [Test]
    public void Peek_Empty()
    {
        var q = new PriorityQueue<int>();

        Assert.Throws<InvalidOperationException>(() => q.Peek());
    }
    
    [Test]
    public void TryPeek_Empty()
    {
        var q = new PriorityQueue<int>();

        Assert.IsFalse(q.TryPeek(out _));
    }
    
    [Test]
    public void Enqueue_Clear_Count()
    {
        var q = new PriorityQueue<int>();

        Assert.AreEqual(0, q.Count);
        
        q.Enqueue(11);
        
        Assert.AreEqual(1, q.Count);
        
        q.Clear();

        Assert.AreEqual(0, q.Count);
    }
    
    [Test]
    public void Enqueue_SameItem()
    {
        var q = new PriorityQueue<int>();
        
        q.Enqueue(11);
        q.Enqueue(11);
        
        Assert.AreEqual(2, q.Count);
        Assert.AreEqual(11, q.Pop());
        Assert.AreEqual(11, q.Pop());
    }
    
    [Test]
    public void RemoveWhere()
    {
        var q = new PriorityQueue<int>();
        
        q.Enqueue(3);
        q.Enqueue(11);

        q.RemoveWhere(v => v.Equals(3));
        
        Assert.AreEqual(1, q.Count);
        Assert.AreEqual(11, q.Pop());
    }
}