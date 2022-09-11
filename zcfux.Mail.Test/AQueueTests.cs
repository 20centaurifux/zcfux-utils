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
using zcfux.Data;
using zcfux.Filter;
using zcfux.Mail.Queue;

namespace zcfux.Mail.Test;

public abstract class AQueueTests
{
    static readonly string TempDir = Path.Combine(
        Path.GetTempPath(),
        typeof(AQueueTests).FullName!);

    IEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();

        Directory.CreateDirectory(TempDir);
    }

    [TearDown]
    public void Teardown()
        => Directory.Delete(TempDir, recursive: true);

    protected abstract IEngine CreateAndSetupEngine();


    QueueStore CreateQueueStore(object handle)
    {
        var db = CreateDb();
        var queueStore = new QueueStore(db, handle);

        return queueStore;
    }

    Store.Store CreateStore(object handle)
    {
        var db = CreateDb();
        var store = new Store.Store(db, handle);

        return store;
    }

    [Test]
    public void CreateQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            Assert.Greater(queue.Id, 0);
            Assert.AreEqual(name, queue.Name);
        }
    }

    [Test]
    public void CreateQueueTwice()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var first = queueStore.NewQueue(name);
            var second = queueStore.NewQueue(name);

            Assert.AreNotEqual(first.Id, second.Id);
            Assert.AreEqual(first.Name, second.Name);
        }
    }

    [Test]
    public void GetQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var received = queueStore.GetQueue(queue.Id);

            Assert.AreEqual(queue.Id, received.Id);
            Assert.AreEqual(queue.Name, received.Name);
        }
    }

    [Test]
    public void GetDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            queue.Delete();

            Assert.Throws<QueueNotFoundException>(() =>
            {
                queueStore.GetQueue(queue.Id);
            });
        }
    }

    [Test]
    public void GetQueues()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var firstName = TestContext.CurrentContext.Random.GetString();

            var first = queueStore.NewQueue(firstName);

            var secondName = TestContext.CurrentContext.Random.GetString();

            var second = queueStore.NewQueue(secondName);

            var queues = queueStore.GetQueues().ToArray();

            Assert.AreEqual(2, queues.Length);

            Assert.IsTrue(queues.Any(q => q.Id == first.Id && q.Name == first.Name));
            Assert.IsTrue(queues.Any(q => q.Id == second.Id && q.Name == second.Name));
        }
    }

    [Test]
    public void DeleteQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            queue.Delete();

            var queues = queueStore.GetQueues();

            Assert.IsEmpty(queues);
        }
    }

    [Test]
    public void RenameQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var newName = TestContext.CurrentContext.Random.GetString();

            queue.Rename(newName);

            Assert.AreEqual(newName, queue.Name);

            var received = queueStore
                .GetQueues()
                .Single(q => q.Id == queue.Id);

            Assert.AreEqual(queue.Name, received.Name);
        }
    }

    [Test]
    public void RenameDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            queue.Delete();

            var newName = TestContext.CurrentContext.Random.GetString();

            Assert.Throws<QueueNotFoundException>(() =>
            {
                queue.Rename(newName);
            });
        }
    }

    [Test]
    public void DeleteAlreadyDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            queue.Delete();

            Assert.Throws<QueueNotFoundException>(() =>
            {
                queue.Delete();
            });
        }
    }

    [Test]
    public void InsertIntoQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var nextDue = DateTime.UtcNow.AddMinutes(10);

            var builder = queue.CreateMessageBuilder()
                .WithTimeToLive(TimeSpan.FromSeconds(23))
                .WithNextDue(nextDue);

            var now = DateTime.UtcNow;

            var queuedMessage = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Carol", "carol@example.org")
                .WithCc("Dave", "dave@example.org")
                .WithCc("Mallory", "mallory@example.org")
                .WithBcc("Oscar", "oscar@example.org")
                .WithBcc("Peggy", "peggy@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .WithHtmlBody("<p>hello world</p>")
                .Store();

            Assert.Greater(queue.Id, 0);
            Assert.AreEqual(0, queuedMessage.Errors);
            Assert.Less((queuedMessage.Created - now).TotalSeconds, 1);
            Assert.Less((queuedMessage.NextDue - nextDue)!.Value.TotalSeconds, 1);
            Assert.GreaterOrEqual((queuedMessage.EndOfLife - now).TotalSeconds, 23);

            Assert.Greater(queuedMessage.Id, 0);

            Assert.AreEqual(new Address("Alice", "alice@example.org"), queuedMessage.From);

            var receivers = queuedMessage.To.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Bob", "bob@example.org"), receivers);
            Assert.Contains(new Address("Carol", "carol@example.org"), receivers);

            receivers = queuedMessage.Cc.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Dave", "dave@example.org"), receivers);
            Assert.Contains(new Address("Mallory", "mallory@example.org"), receivers);

            receivers = queuedMessage.Bcc.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Oscar", "oscar@example.org"), receivers);
            Assert.Contains(new Address("Peggy", "peggy@example.org"), receivers);

            Assert.AreEqual("test", queuedMessage.Subject);
            Assert.AreEqual("hello world", queuedMessage.TextBody);
            Assert.AreEqual("<p>hello world</p>", queuedMessage.HtmlBody);
        }
    }

    [Test]
    public void CreateMessageBuilderWithDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world");

            queue.Delete();

            Assert.Throws<QueueNotFoundException>(() =>
            {
                builder.Store();
            });
        }
    }

    [Test]
    public void InsertIntoDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            queue.Delete();

            Assert.Throws<QueueNotFoundException>(() =>
            {
                queue.CreateMessageBuilder();
            });
        }
    }

    [Test]
    public void NextQueuedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            var queuedMessage = builder
                .WithTimeToLive(TimeSpan.FromMinutes(1))
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var next);

            Assert.IsTrue(success);

            CompareQueuedMessages(queuedMessage, next!);
        }
    }

    [Test]
    public void DeleteQueuedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            queuedMessage!.Delete();

            success = queue.TryGetNext(out var _);

            Assert.IsFalse(success);
        }
    }

    [Test]
    public void DeleteAlreadyDeletedQueuedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            queuedMessage!.Delete();

            Assert.Throws(
                Is.TypeOf<QueueItemNotFoundException>().Or.TypeOf<MessageNotFoundException>(),
                () => queuedMessage.Delete());
        }
    }

    [Test]
    public void DeleteMovedQueuedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            var store = CreateStore(t.Handle);

            var directoryName = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(directoryName);

            queuedMessage!.MoveToStore(directory);

            Assert.Throws(
                Is.TypeOf<QueueItemNotFoundException>().Or.TypeOf<MessageNotFoundException>(),
                () => queuedMessage.Delete());
        }
    }

    [Test]
    public void DeleteQueuedMessageWithDeletedQueue()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            queue.Delete();

            Assert.Throws(
                Is.TypeOf<QueueNotFoundException>().Or.TypeOf<QueueItemNotFoundException>(),
                () => queuedMessage!.Delete());
        }
    }

    [Test]
    public void SetQueuedMessageFailed()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var first);

            Assert.IsTrue(success);

            first!.Fail(TimeSpan.FromMinutes(1));

            Assert.AreEqual(1, first.Errors);
            Assert.AreEqual(Math.Round((first.NextDue - DateTime.UtcNow)!.Value.TotalSeconds), 60.0);

            success = queue.TryGetNext(out var second);

            Assert.IsTrue(success);

            CompareQueuedMessages(first, second!);
        }
    }

    [Test]
    public void SetDeletedQueuedMessageFailed()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            queuedMessage!.Delete();

            Assert.Throws<QueueItemNotFoundException>(() =>
            {
                queuedMessage.Fail(TimeSpan.FromMinutes(1));
            });
        }
    }

    [Test]
    public void SetMovedQueuedMessageFailed()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var queueName = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(queueName);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            var store = CreateStore(t.Handle);

            var directoryName = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(directoryName);

            queuedMessage!.MoveToStore(directory);

            Assert.Throws<QueueItemNotFoundException>(() =>
            {
                queuedMessage.Fail(TimeSpan.FromMinutes(1));
            });
        }
    }

    [Test]
    public void SetQueuedMessageWithDeletedQueueFailed()
    {
        using (var t = _engine.NewTransaction())
        {
            var queueStore = CreateQueueStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var queue = queueStore.NewQueue(name);

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world")
                .Store();

            var success = queue.TryGetNext(out var queuedMessage);

            Assert.IsTrue(success);

            queue.Delete();

            Assert.Throws(
                Is.TypeOf<QueueNotFoundException>().Or.TypeOf<QueueItemNotFoundException>(),
                () => queuedMessage!.Fail(TimeSpan.FromMinutes(1)));
        }
    }

    [Test]
    public void Query_QueueId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.QueueId.EqualTo(firstQueue.Id));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void OrderBy_QueueId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.QueueId);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            var expectedFirstMessage = (secondQueue.Id > firstQueue.Id)
                ? second
                : first;

            CompareQueuedMessages(expectedFirstMessage, fetched[0]);
        }
    }

    [Test]
    public void Query_Queue()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Queue.EqualTo(firstQueue.Name));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void OrderBy_Queue()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Queue);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_From()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.From.Contains("Bob"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, second);
        }
    }

    [Test]
    public void Order_From()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.From);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_To()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.To.Contains("Carol"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_To()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithTo("Victor", "victor@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.To);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_Cc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithCc("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithCc("Oscar", "oscar@example.org")
                .WithCc("Peggy", "peggy@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Cc.Contains("Carol"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_Cc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithCc("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithCc("Oscar", "oscar@example.org")
                .WithCc("Peggy", "peggy@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Cc);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_Bcc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithBcc("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithBcc("Oscar", "oscar@example.org")
                .WithBcc("Peggy", "peggy@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Bcc.Contains("Carol"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_Bcc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithBcc("Carol", "carol@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithBcc("Oscar", "oscar@example.org")
                .WithBcc("Peggy", "peggy@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Bcc);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_Subject()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Subject.EqualTo("hello"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_Subject()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Subject);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_TextBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.TextBody.EqualTo("hello"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_TextBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.TextBody);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_HtmlBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            var first = firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithHtmlBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithHtmlBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.HtmlBody.EqualTo("hello"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_HtmlBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var firstQueue = store.NewQueue("a");

            firstQueue.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithHtmlBody("hello")
                .Store();

            var secondQueue = store.NewQueue("b");

            var second = secondQueue.CreateMessageBuilder()
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithHtmlBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.HtmlBody);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
        }
    }

    [Test]
    public void Query_AttachmentId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var fooPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(fooPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithAttachment(fooPath)
                .Store();

            var barPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(barPath, "bar");

            builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithAttachment(barPath)
                .Store();

            var attachmentId = first
                .GetAttachments()
                .Single()
                .Id;

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.AttachmentId.EqualTo(attachmentId));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_AttachmentId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var fooPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(fooPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithAttachment(fooPath)
                .Store();

            var firstAttachmentId = first
                .GetAttachments()
                .Single()
                .Id;

            var barPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(barPath, "bar");

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithAttachment(barPath)
                .Store();

            var secondAttachmentId = second
                .GetAttachments()
                .Single()
                .Id;

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.AttachmentId);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            if (firstAttachmentId > secondAttachmentId)
            {
                CompareQueuedMessages(first, fetched[0]);
                CompareQueuedMessages(second, fetched[1]);
            }
            else
            {
                CompareQueuedMessages(second, fetched[0]);
                CompareQueuedMessages(first, fetched[1]);
            }
        }
    }

    [Test]
    public void Query_Attachment()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var fooPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(fooPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithAttachment(fooPath)
                .Store();

            var barPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(barPath, "bar");

            builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithAttachment(barPath)
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Attachment.EqualTo("foo.txt"));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_Attachment()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var fooPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(fooPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithAttachment(fooPath)
                .Store();

            var barPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(barPath, "bar");

            builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithAttachment(barPath)
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Attachment);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(first, fetched[0]);
        }
    }

    [Test]
    public void Query_Created()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Created.LessThan(second.Created));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_Created()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Created);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
            CompareQueuedMessages(first, fetched[1]);
        }
    }
    
    [Test]
    public void Query_EndOfLife()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithTimeToLive(TimeSpan.FromMinutes((1)))
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithTimeToLive(TimeSpan.FromMinutes((5)))
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.EndOfLife.LessThan(second.EndOfLife));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_EndOfLife()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithTimeToLive(TimeSpan.FromMinutes((1)))
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithTimeToLive(TimeSpan.FromMinutes((1)))
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.Created);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
            CompareQueuedMessages(first, fetched[1]);
        }
    }
    
    [Test]
    public void Query_NextDue()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .WithNextDue(DateTime.UtcNow)
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .WithNextDue(DateTime.UtcNow.Add(TimeSpan.FromMinutes((1))))
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.NextDue.LessThan(second.NextDue!.Value));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, first);
        }
    }

    [Test]
    public void Order_NextDue()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.NextDue);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
            CompareQueuedMessages(first, fetched[1]);
        }
    }

    [Test]
    public void Query_Errors()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            second.Fail(TimeSpan.FromMinutes(1));
            
            var qb = new QueryBuilder()
                .WithFilter(QueuedMessageFilters.Errors.EqualTo(1));

            var fetched = store.Query(qb.Build()).Single();

            CompareQueuedMessages(fetched, second);
        }
    }

    [Test]
    public void Order_Errors()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateQueueStore(t.Handle);

            var queue = store.NewQueue("a");

            var builder = queue.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store(); ;

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithOrderByDescending(QueuedMessageFilters.NextDue);

            var fetched = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, fetched.Length);

            CompareQueuedMessages(second, fetched[0]);
            CompareQueuedMessages(first, fetched[1]);
        }
    }
    
    void CompareQueuedMessages(QueuedMessage a, QueuedMessage b)
    {
        Assert.AreEqual(a.Id, b.Id);
        Assert.AreEqual(a.From, b.From);
        Assert.IsTrue(a.To.SequenceEqual(b.To));
        Assert.IsTrue(a.Cc.SequenceEqual(b.Cc));
        Assert.IsTrue(a.Bcc.SequenceEqual(b.Bcc));
        Assert.AreEqual(a.Subject, b.Subject);
        Assert.AreEqual(a.TextBody, b.TextBody);
        Assert.AreEqual(a.HtmlBody, b.HtmlBody);

        Assert.IsTrue(
            a.GetAttachments().Select(attachment => attachment.Id).ToArray()
                .SequenceEqual(b.GetAttachments().Select(attachment => attachment.Id)));

        Assert.IsTrue(
            a.GetAttachments().Select(attachment => attachment.Filename).ToArray()
                .SequenceEqual(b.GetAttachments().Select(attachment => attachment.Filename)));

        Assert.Less(Math.Abs((a.Created - b.Created).TotalSeconds), 1);
        Assert.Less(Math.Abs((a.EndOfLife - b.EndOfLife).TotalSeconds), 1);

        Assert.AreEqual(a.NextDue.HasValue, b.NextDue.HasValue);

        if (a.NextDue.HasValue)
        {
            Assert.Less(Math.Abs((a.NextDue - b.NextDue)!.Value.TotalSeconds), 1);
        }

        Assert.AreEqual(a.Errors, b.Errors);
    }

    protected abstract IDb CreateDb();
}