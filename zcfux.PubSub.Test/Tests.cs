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
using System.Collections.Concurrent;
using NUnit.Framework;

namespace zcfux.PubSub.Test;

public sealed class Tests
{
    record PrivateMessage(Subscriber From, string Text);

    [Test]
    public void Subscribe()
    {
        var channel = new Channel<string>();

        var alice = new Subscriber("Alice");

        channel.Subscribe(alice, _ => { });

        var subscribers = channel.GetSubscribers();

        Assert.AreEqual(alice, subscribers.Single());
    }

    [Test]
    public void SubscribeTwice()
    {
        var channel = new Channel<string>();

        var alice = new Subscriber("Alice");

        var firstHandler = false;

        channel.Subscribe(alice, _ =>
        {
            firstHandler = true;
        });

        var secondHandler = false;

        channel.Subscribe(alice, _ =>
        {
            secondHandler = true;
        });

        var subscribers = channel.GetSubscribers();

        Assert.AreEqual(alice, subscribers.Single());

        channel.BroadcastAsync(TestContext.CurrentContext.Random.GetString()).Wait();

        Assert.IsFalse(firstHandler);
        Assert.IsTrue(secondHandler);
    }

    [Test]
    public void ReceiveSubscribedEvent()
    {
        var channel = new Channel<string>();

        var counter = 0;

        var alice = new Subscriber("Alice");

        channel.Subscribed += (_, e) =>
        {
            if (e.Subscriber.Equals(alice))
            {
                counter++;
            }
        };

        channel.Subscribe(alice, _ => { });

        Assert.AreEqual(1, counter);

        channel.Subscribe(alice, _ => { });

        Assert.AreEqual(2, counter);
    }

    [Test]
    public void Unsubscribe()
    {
        var channel = new Channel<string>();

        channel.Subscribe("Alice", _ => { });

        channel.Unsubscribe("Alice");

        var subscribers = channel.GetSubscribers();

        Assert.IsEmpty(subscribers);
    }

    [Test]
    public void UnsubscribeTwice()
    {
        var channel = new Channel<string>();

        var alice = new Subscriber("Alice");

        channel.Subscribe(alice, _ => { });

        channel.Unsubscribe("Alice");
        channel.Unsubscribe("Alice");

        var subscribers = channel.GetSubscribers();

        Assert.IsEmpty(subscribers);
    }

    [Test]
    public void ReceiveUnsubscribedEvent()
    {
        var channel = new Channel<string>();

        var counter = 0;

        var alice = new Subscriber("Alice");

        channel.Unsubscribed += (_, e) =>
        {
            if (e.Subscriber.Equals(alice))
            {
                counter++;
            }
        };

        channel.Subscribe(alice, _ => { });

        channel.Unsubscribe(alice);

        Assert.AreEqual(1, counter);

        channel.Unsubscribe(alice);

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void Broadcast()
    {
        var channel = new Channel<string>();

        var bag = new ConcurrentBag<(Subscriber, string)>();

        var alice = new Subscriber("Alice");

        channel.Subscribe(alice, msg =>
        {
            bag.Add((alice, msg));
        });

        var bob = new Subscriber("Bob");

        channel.Subscribe(bob, msg =>
        {
            bag.Add((bob, msg));
        });

        var message = TestContext.CurrentContext.Random.GetString();

        channel.BroadcastAsync(message).Wait();

        Assert.AreEqual(2, bag.Count);

        Assert.IsTrue(bag.Any(t => t.Item1.Equals(alice) && t.Item2.Equals(message)));
        Assert.IsTrue(bag.Any(t => t.Item1.Equals(bob) && t.Item2.Equals(message)));
    }

    [Test]
    public void BroadcastWithTimeout()
    {
        var options = new ChannelOptions(MaxConcurrentTasks: 1, TaskBlockingTimeout: TimeSpan.FromSeconds(1));

        var channel = new Channel<string>(options);

        long count = 0;

        channel.Subscribe("Alice", _ =>
        {
            Interlocked.Increment(ref count);

            Thread.Sleep(TimeSpan.FromSeconds(2));
        });

        var tasks = new[]
        {
            channel.BroadcastAsync(TestContext.CurrentContext.Random.GetString()),
            channel.BroadcastAsync(TestContext.CurrentContext.Random.GetString())
        };

        Assert.Throws<AggregateException>(() => Task.WaitAll(tasks));

        Assert.AreEqual(1, count);
    }

    [Test]
    public void BroadcastWithInfiniteTimeout()
    {
        var options = new ChannelOptions(MaxConcurrentTasks: 1, TaskBlockingTimeout: TimeSpan.Zero);

        var channel = new Channel<string>(options);

        long count = 0;

        channel.Subscribe("Alice", _ =>
        {
            Interlocked.Increment(ref count);

            Thread.Sleep(TimeSpan.FromSeconds(1));
        });

        var tasks = new[]
        {
            channel.BroadcastAsync(TestContext.CurrentContext.Random.GetString()),
            channel.BroadcastAsync(TestContext.CurrentContext.Random.GetString())
        };

        Task.WaitAll(tasks);

        Assert.AreEqual(2, count);
    }

    [Test]
    public void SendTo()
    {
        var channel = new Channel<PrivateMessage>();

        var alice = new Subscriber("Alice");
        var alicesInbox = new List<PrivateMessage>();

        channel.Subscribe(alice, msg =>
        {
            alicesInbox.Add(msg);
        });

        var bob = new Subscriber("Bob");
        var bobsInbox = new List<PrivateMessage>();

        channel.Subscribe(bob, msg =>
        {
            bobsInbox.Add(msg);
        });

        var alicesMessage = TestContext.CurrentContext.Random.GetString();

        channel.SendToAsync(bob, new PrivateMessage(alice, alicesMessage)).Wait();

        Assert.AreEqual(0, alicesInbox.Count);
        Assert.AreEqual(1, bobsInbox.Count);

        var (sender, text) = bobsInbox.First();

        Assert.AreEqual(alice, sender);
        Assert.AreEqual(alicesMessage, text);

        var bobsResponse = TestContext.CurrentContext.Random.GetString();

        channel.SendToAsync(alice, new PrivateMessage(bob, bobsResponse)).Wait();

        Assert.AreEqual(1, alicesInbox.Count);
        Assert.AreEqual(1, bobsInbox.Count);

        (sender, text) = alicesInbox.First();

        Assert.AreEqual(bob, sender);
        Assert.AreEqual(bobsResponse, text);
    }

    [Test]
    public void SendToWithTimeout()
    {
        var options = new ChannelOptions(MaxConcurrentTasks: 1, TaskBlockingTimeout: TimeSpan.FromSeconds(1));

        var channel = new Channel<string>(options);

        long count = 0;

        channel.Subscribe("Alice", _ =>
        {
            Interlocked.Increment(ref count);

            Thread.Sleep(TimeSpan.FromSeconds(2));
        });

        var tasks = new[]
        {
            channel.SendToAsync("Alice", TestContext.CurrentContext.Random.GetString()),
            channel.SendToAsync("Alice", TestContext.CurrentContext.Random.GetString())
        };

        Assert.Throws<AggregateException>(() => Task.WaitAll(tasks));

        Assert.AreEqual(1, count);
    }

    [Test]
    public void SendToWithInfiniteTimeout()
    {
        var options = new ChannelOptions(MaxConcurrentTasks: 1, TaskBlockingTimeout: TimeSpan.Zero);

        var channel = new Channel<string>(options);

        long count = 0;

        channel.Subscribe("Alice", _ =>
        {
            Interlocked.Increment(ref count);

            Thread.Sleep(TimeSpan.FromSeconds(1));
        });

        var tasks = new[]
        {
            channel.SendToAsync("Alice", TestContext.CurrentContext.Random.GetString()),
            channel.SendToAsync("Alice", TestContext.CurrentContext.Random.GetString())
        };

        Task.WaitAll(tasks);

        Assert.AreEqual(2, count);
    }
}