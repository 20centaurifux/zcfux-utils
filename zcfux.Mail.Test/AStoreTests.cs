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
using zcfux.Mail.Store;
using Directory = System.IO.Directory;
using DirectoryNotFoundException = zcfux.Mail.Store.DirectoryNotFoundException;

namespace zcfux.Mail.Test;

public abstract class AStoreTests
{
    static readonly string TempDir = Path.Combine(
        Path.GetTempPath(),
        typeof(AStoreTests).FullName!);

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

    [Test]
    public void CreateDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(name);

            Assert.Greater(directory.Id, 0);
            Assert.AreEqual(name, directory.Name);
            Assert.IsNull(directory.GetParent());
            Assert.IsEmpty(directory.GetChildren());
        }
    }

    [Test]
    public void CreateDirectoryTwice()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            store.NewDirectory(name);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                store.NewDirectory(name);
            });
        }
    }

    [Test]
    public void GetDirectories()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var firstName = TestContext.CurrentContext.Random.GetString();

            var first = store.NewDirectory(firstName);

            var secondName = TestContext.CurrentContext.Random.GetString();

            var second = store.NewDirectory(secondName);

            var directories = store.GetDirectories().ToArray();

            Assert.AreEqual(2, directories.Length);
            Assert.IsTrue(directories.Any(d => d.Id == first.Id && d.Name == first.Name));
            Assert.IsTrue(directories.Any(d => d.Id == second.Id && d.Name == second.Name));
        }
    }

    [Test]
    public void RenameDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(name);

            name = TestContext.CurrentContext.Random.GetString();

            directory.Rename(name);

            Assert.AreEqual(directory.Name, name);

            var directories = store.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(directories.Any(d => d.Id == directory.Id && d.Name == directory.Name));
        }
    }

    [Test]
    public void RenameDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(name);

            name = TestContext.CurrentContext.Random.GetString();

            directory.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                directory.Rename(name);
            });
        }
    }

    [Test]
    public void RenameDirectoryWithConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var firstName = TestContext.CurrentContext.Random.GetString();

            var first = store.NewDirectory(firstName);

            var secondName = TestContext.CurrentContext.Random.GetString();

            store.NewDirectory(secondName);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                first.Rename(secondName);
            });
        }
    }

    [Test]
    public void DeleteDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var firstName = TestContext.CurrentContext.Random.GetString();

            var first = store.NewDirectory(firstName);

            var secondName = TestContext.CurrentContext.Random.GetString();

            var second = store.NewDirectory(secondName);

            first.Delete();

            var directories = store.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(directories.Any(d => d.Id == second.Id && d.Name == second.Name));
        }
    }

    [Test]
    public void DeleteAlreadyDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(name);

            directory.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                directory.Delete();
            });
        }
    }

    [Test]
    public void CreateSubdirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var childName = TestContext.CurrentContext.Random.GetString();

            var child = parent.NewChild(childName);

            Assert.Greater(child.Id, 0);
            Assert.AreNotEqual(child.Id, parent.Id);
            Assert.AreEqual(child.Name, childName);
            Assert.AreEqual(child.GetParent()!.Id, parent.Id);
            Assert.AreEqual(child.GetParent()!.Name, parent.Name);
            Assert.IsEmpty(child.GetChildren());

            var children = parent.GetChildren().ToArray();

            Assert.AreEqual(1, children.Length);
            Assert.AreEqual(child.Id, children[0].Id);
            Assert.AreEqual(child.Name, children[0].Name);
            Assert.AreEqual(child.GetParent()!.Id, children[0].GetParent()!.Id);
            Assert.AreEqual(child.GetParent()!.Name, children[0].GetParent()!.Name);
            Assert.IsEmpty(children[0].GetChildren());
        }
    }

    [Test]
    public void CreateSubdirectoryTwice()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var childName = TestContext.CurrentContext.Random.GetString();

            parent.NewChild(childName);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                parent.NewChild(childName);
            });
        }
    }

    [Test]
    public void CreateSubdirectoryWithDeletedParent()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var childName = TestContext.CurrentContext.Random.GetString();

            parent.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                parent.NewChild(childName);
            });
        }
    }

    [Test]
    public void GetSubdirectories()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var firstChildName = TestContext.CurrentContext.Random.GetString();

            var firstChild = parent.NewChild(firstChildName);

            var secondChildName = TestContext.CurrentContext.Random.GetString();

            var secondChild = parent.NewChild(secondChildName);

            var children = parent.GetChildren().ToArray();

            Assert.AreEqual(2, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == firstChild.Id && d.Name == firstChild.Name));
            Assert.IsTrue(children.Any(d => d.Id == secondChild.Id && d.Name == secondChild.Name));
        }
    }

    [Test]
    public void GetSubdirectoriesOfDeletedParent()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var childName = TestContext.CurrentContext.Random.GetString();

            parent.NewChild(childName);

            parent.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                parent.GetChildren();
            });
        }
    }

    [Test]
    public void RenameSubdirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var childName = TestContext.CurrentContext.Random.GetString();

            var child = parent.NewChild(childName);

            childName = TestContext.CurrentContext.Random.GetString();

            child.Rename(childName);

            Assert.AreEqual(child.Name, childName);

            var children = parent.GetChildren().ToArray();

            Assert.AreEqual(1, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == child.Id && d.Name == child.Name));
        }
    }

    [Test]
    public void RenameSubdirectoryWithConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var firstChildName = TestContext.CurrentContext.Random.GetString();

            var firstChild = parent.NewChild(firstChildName);

            var secondChildName = TestContext.CurrentContext.Random.GetString();

            parent.NewChild(secondChildName);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                firstChild.Rename(secondChildName);
            });
        }
    }

    [Test]
    public void DeleteSubdirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var parentName = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(parentName);

            var firstChildName = TestContext.CurrentContext.Random.GetString();

            var firstChild = parent.NewChild(firstChildName);

            var secondChildName = TestContext.CurrentContext.Random.GetString();

            var secondChild = parent.NewChild(secondChildName);

            secondChild.Delete();

            var children = parent.GetChildren().ToArray();

            Assert.AreEqual(1, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == firstChild.Id && d.Name == firstChild.Name));
        }
    }

    [Test]
    public void MoveDirectoryToRoot()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var b = a.NewChild("b");

            b.MoveToRoot();

            Assert.IsNull(b.GetParent());

            var children = store.GetDirectories().ToArray();

            Assert.AreEqual(2, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == a.Id && d.Name == a.Name));
            Assert.IsTrue(children.Any(d => d.Id == b.Id && d.Name == b.Name));
        }
    }

    [Test]
    public void MoveDirectoryToRootWithConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(name);

            var child = parent.NewChild(parent.Name);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                child.MoveToRoot();
            });
        }
    }

    [Test]
    public void MoveDeletedDirectoryToRoot()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var parent = store.NewDirectory(name);

            var child = parent.NewChild(parent.Name);

            child.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                child.MoveToRoot();
            });
        }
    }

    [Test]
    public void MoveDirectoryToParent()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var c = a.NewChild("c");

            var b = store.NewDirectory("b");

            c.Move(b);

            Assert.AreEqual(c.GetParent()!.Id, b.Id);
            Assert.AreEqual(c.GetParent()!.Name, b.Name);

            var children = a.GetChildren().ToArray();

            Assert.AreEqual(0, children.Length);

            children = b.GetChildren().ToArray();

            Assert.AreEqual(1, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == c.Id && d.Name == c.Name));
        }
    }

    [Test]
    public void MoveDirectoryToParentWithConflict()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var grandParent = store.NewDirectory(name);

            var parent = grandParent.NewChild(grandParent.Name);

            var child = parent.NewChild(parent.Name);

            Assert.Throws<DirectoryAlreadyExistsException>(() =>
            {
                child.Move(grandParent);
            });
        }
    }

    [Test]
    public void MoveDirectoryToChild()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var b = a.NewChild("b");

            var c = b.NewChild("c");

            Assert.Throws<ArgumentException>(() =>
            {
                a.Move(c);
            });
        }
    }

    [Test]
    public void MoveDirectoryToSelf()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            Assert.Throws<ArgumentException>(() =>
            {
                a.Move(a);
            });
        }
    }

    [Test]
    public void MoveDirectoryDeletedParent()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            a.Delete();

            var b = store.NewDirectory("b");
            var c = b.NewChild("c");

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                c.Move(a);
            });
        }
    }

    [Test]
    public void MoveDeletedDirectoryToParent()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");
            var b = store.NewDirectory("b");
            var c = b.NewChild("c");

            c.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                c.Move(a);
            });
        }
    }

    [Test]
    public void MoveDirectoryToSameLocation()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var b = a.NewChild("b");

            b.Move(a);

            Assert.AreEqual(b.GetParent()!.Id, a.Id);
            Assert.AreEqual(b.GetParent()!.Name, a.Name);

            var children = a.GetChildren().ToArray();

            Assert.AreEqual(1, children.Length);
            Assert.IsTrue(children.Any(d => d.Id == b.Id && d.Name == b.Name));
        }
    }

    [Test]
    public void CreateMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var attachmentPath = Path.Combine(TempDir, "test.txt");

            File.WriteAllText(attachmentPath, "foo bar");

            var message = builder
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
                .WithAttachment(attachmentPath)
                .Store();

            Assert.Greater(message.Id, 0);

            Assert.AreEqual(new Address("Alice", "alice@example.org"), message.From);

            var receivers = message.To.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Bob", "bob@example.org"), receivers);
            Assert.Contains(new Address("Carol", "carol@example.org"), receivers);

            receivers = message.Cc.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Dave", "dave@example.org"), receivers);
            Assert.Contains(new Address("Mallory", "mallory@example.org"), receivers);

            receivers = message.Bcc.ToArray();

            Assert.AreEqual(2, receivers.Length);
            Assert.Contains(new Address("Oscar", "oscar@example.org"), receivers);
            Assert.Contains(new Address("Peggy", "peggy@example.org"), receivers);

            Assert.AreEqual("test", message.Subject);
            Assert.AreEqual("hello world", message.TextBody);
            Assert.AreEqual("<p>hello world</p>", message.HtmlBody);

            var attachment = message.GetAttachments().Single();

            Assert.Greater(attachment.Id, 0);
            Assert.AreEqual("test.txt", attachment.Filename);

            using (var stream = attachment.OpenRead())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();

                    Assert.AreEqual("foo bar", content);
                }
            }
        }
    }

    [Test]
    public void CreateMessageBuilderWithDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            directory.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                directory.CreateMessageBuilder();
            });
        }
    }

    [Test]
    public void CreateMessageInDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder()
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello world");

            directory.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                builder.Store();
            });
        }
    }

    [Test]
    public void GetMailsFromEmptyDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var result = directory.GetMessages();

            Assert.AreEqual(0, result.Count());
        }
    }

    [Test]
    public void GetMailsFromDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var firstMessage = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondMessage = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            var result = directory
                .GetMessages()
                .ToArray();

            Assert.AreEqual(2, result.Length);

            var fetched = result.Single(msg => msg.Id == firstMessage.Id);

            CompareMessages(firstMessage, fetched);

            fetched = result.Single(msg => msg.Id == secondMessage.Id);

            CompareMessages(secondMessage, fetched);
        }
    }

    [Test]
    public void GetMailsFromDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            directory.Delete();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                directory.GetMessages().ToArray();
            });
        }
    }

    [Test]
    public void DeleteMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var firstMessage = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            var secondMessage = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("world")
                .WithTextBody("world")
                .Store();

            firstMessage.Delete();

            var result = directory
                .GetMessages()
                .ToArray();

            Assert.AreEqual(1, result.Length);

            var fetched = result.SingleOrDefault(msg => msg.Id == firstMessage.Id);

            Assert.IsNull(fetched);

            fetched = result.Single(msg => msg.Id == secondMessage.Id);

            CompareMessages(secondMessage, fetched);
        }
    }

    [Test]
    public void DeleteAlreadyDeletedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var message = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("hello")
                .Store();

            message.Delete();

            Assert.Throws<DirectoryEntryNotFoundException>(() =>
            {
                message.Delete();
            });
        }
    }

    [Test]
    public void MoveMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var firstDirectory = store.NewDirectory(name);

            name = TestContext.CurrentContext.Random.GetString();

            var secondDirectory = store.NewDirectory(name);

            var builder = firstDirectory.CreateMessageBuilder();

            var message = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello world")
                .WithTextBody("hello world")
                .Store();

            message.Move(secondDirectory);

            var messages = firstDirectory.GetMessages().ToArray();

            Assert.AreEqual(0, messages.Length);

            messages = secondDirectory.GetMessages().ToArray();

            Assert.AreEqual(1, messages.Length);

            CompareMessages(message, messages.First());
        }
    }

    [Test]
    public void MoveMessageToSameLocation()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var directory = store.NewDirectory(name);

            var builder = directory.CreateMessageBuilder();

            var message = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello world")
                .WithTextBody("hello world")
                .Store();

            message.Move(directory);

            var messages = directory.GetMessages().ToArray();

            Assert.AreEqual(1, messages.Length);

            CompareMessages(message, messages.First());
        }
    }

    [Test]
    public void MoveMessageToDeletedDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var firstDirectory = store.NewDirectory(name);

            name = TestContext.CurrentContext.Random.GetString();

            var secondDirectory = store.NewDirectory(name);

            secondDirectory.Delete();

            var builder = firstDirectory.CreateMessageBuilder();

            var message = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello world")
                .WithTextBody("hello world")
                .Store();

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                message.Move(secondDirectory);
            });
        }
    }

    [Test]
    public void MoveDeletedMessage()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var name = TestContext.CurrentContext.Random.GetString();

            var firstDirectory = store.NewDirectory(name);

            name = TestContext.CurrentContext.Random.GetString();

            var secondDirectory = store.NewDirectory(name);

            var builder = firstDirectory.CreateMessageBuilder();

            var message = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello world")
                .WithTextBody("hello world")
                .Store();

            message.Delete();

            Assert.Throws<DirectoryEntryNotFoundException>(() =>
            {
                message.Move(secondDirectory);
            });
        }
    }

    [Test]
    public void Query_Id()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Id.EqualTo(first.Id));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_DirectoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var builder = a.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            var b = store.NewDirectory("b");

            builder = b.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(b.Id));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Query_Directory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var builder = a.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            var b = store.NewDirectory("b");

            builder = b.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Directory.EqualTo(a.Name));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_From()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.From.Contains("alice@example.org"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_To()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Peggy", "peggy@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.To.Contains("bob@example.org"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_Cc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Peggy", "peggy@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithCc("Carol", "carol@example.org")
                .WithCc("Chuck", "chuck@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Cc.Contains("chuck@example.org"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Query_Bcc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithTo("Peggy", "peggy@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithBcc("Carol", "carol@example.org")
                .WithBcc("Chuck", "chuck@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Bcc.Contains("carol@example.org"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Query_Subject()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Subject.EqualTo("hello"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_TextBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("hello")
                .WithTextBody("world")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.TextBody.EqualTo("bar"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Query_HtmlBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello world</p>")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("bar")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.HtmlBody.IsNull());

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Query_AttachmentId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var attachmentPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(attachmentPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("foo")
                .WithAttachment(attachmentPath)
                .Store();

            var attachmentId = first
                .GetAttachments()
                .Single()
                .Id;

            attachmentPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(attachmentPath, "bar");

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("bar")
                .WithTextBody("bar")
                .WithAttachment(attachmentPath)
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.AttachmentId.EqualTo(attachmentId));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, first);
        }
    }

    [Test]
    public void Query_Attachment()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var attachmentPath = Path.Combine(TempDir, "test.txt");

            File.WriteAllText(attachmentPath, "hello world");

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("foo")
                .WithAttachment(attachmentPath)
                .Store();

            attachmentPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(attachmentPath, "foo");

            attachmentPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(attachmentPath, "bar");

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("bar")
                .WithTextBody("bar")
                .WithAttachment(attachmentPath)
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.Attachment.EqualTo("bar.txt"));

            var fetched = store.Query(qb.Build()).Single();

            CompareMessages(fetched, second);
        }
    }

    [Test]
    public void Order_ById()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Id);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.Greater(messages[1].Id, messages[0].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.Id);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.Greater(messages[0].Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByDirectoryId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var builder = a.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            var b = store.NewDirectory("b");

            builder = b.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(Logical.Or(
                    StoredMessageFilters.DirectoryId.EqualTo(a.Id),
                    StoredMessageFilters.DirectoryId.EqualTo(b.Id)))
                .WithOrderBy(StoredMessageFilters.DirectoryId);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(Logical.Or(
                    StoredMessageFilters.DirectoryId.EqualTo(a.Id),
                    StoredMessageFilters.DirectoryId.EqualTo(b.Id)))
                .WithOrderByDescending(StoredMessageFilters.DirectoryId);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByDirectory()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var a = store.NewDirectory("a");

            var builder = a.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            var b = store.NewDirectory("b");

            builder = b.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(Logical.Or(
                    StoredMessageFilters.DirectoryId.EqualTo(a.Id),
                    StoredMessageFilters.DirectoryId.EqualTo(b.Id)))
                .WithOrderBy(StoredMessageFilters.Directory);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(Logical.Or(
                    StoredMessageFilters.DirectoryId.EqualTo(a.Id),
                    StoredMessageFilters.DirectoryId.EqualTo(b.Id)))
                .WithOrderByDescending(StoredMessageFilters.Directory);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByFrom()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            builder = directory.CreateMessageBuilder();

            var second = builder
                .WithFrom("Bob", "bob@example.org")
                .WithTo("Alice", "alice@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.From);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.From);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByTo()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Sent");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Trudy", "trudy@example.org")
                .WithTo("Victor", "victor@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            builder = directory.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Frank", "frank@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.To);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.To);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByCc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Sent");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Heidi", "heidi@example.org")
                .WithCc("Trudy", "trudy@example.org")
                .WithCc("Victor", "victor@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            builder = directory.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Heidi", "heidi@example.org")
                .WithCc("Frank", "frank@example.org")
                .WithCc("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Cc);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.Cc);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByBcc()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Sent");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Heidi", "heidi@example.org")
                .WithBcc("Trudy", "trudy@example.org")
                .WithBcc("Victor", "victor@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            builder = directory.CreateMessageBuilder();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Heidi", "heidi@example.org")
                .WithBcc("Frank", "frank@example.org")
                .WithBcc("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Bcc);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.Bcc);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_BySubject()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("a")
                .WithTextBody("hello")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("b")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Subject);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.Subject);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByTextBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("hello")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithTextBody("world")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.TextBody);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.TextBody);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByHtmlBody()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello</p>")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>world</p>")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.HtmlBody);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.HtmlBody);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);
        }
    }

    [Test]
    public void Order_ByAttachmentId()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var attachmentPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(attachmentPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("foo")
                .WithAttachment(attachmentPath)
                .Store();

            var firstAttachmentId = first
                .GetAttachments()
                .Single()
                .Id;

            attachmentPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(attachmentPath, "bar");

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("bar")
                .WithTextBody("bar")
                .WithAttachment(attachmentPath)
                .Store();

            var secondAttachmentId = second
                .GetAttachments()
                .Single()
                .Id;

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.AttachmentId);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);

            if (firstAttachmentId < secondAttachmentId)
            {
                Assert.Greater(
                    messages[1].GetAttachments().Single().Id,
                    messages[0].GetAttachments().Single().Id);
            }
            else
            {
                Assert.Greater(
                    messages[0].GetAttachments().Single().Id,
                    messages[1].GetAttachments().Single().Id);
            }

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.AttachmentId);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);

            if (firstAttachmentId < secondAttachmentId)
            {
                Assert.Greater(
                    messages[0].GetAttachments().Single().Id,
                    messages[1].GetAttachments().Single().Id);
            }
            else
            {
                Assert.Greater(
                    messages[1].GetAttachments().Single().Id,
                    messages[0].GetAttachments().Single().Id);
            }
        }
    }

    [Test]
    public void Order_ByAttachment()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var attachmentPath = Path.Combine(TempDir, "foo.txt");

            File.WriteAllText(attachmentPath, "foo");

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("foo")
                .WithTextBody("foo")
                .WithAttachment(attachmentPath)
                .Store();

            attachmentPath = Path.Combine(TempDir, "bar.txt");

            File.WriteAllText(attachmentPath, "bar");

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("bar")
                .WithTextBody("bar")
                .WithAttachment(attachmentPath)
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Attachment);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(first.Id, messages[1].Id);

            qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderByDescending(StoredMessageFilters.Attachment);

            messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);
        }
    }

    [Test]
    public void Skip()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello</p>")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>world</p>")
                .Store();

            var third = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello world</p>")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Id)
                .WithSkip(1);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
            Assert.AreEqual(third.Id, messages[1].Id);
        }
    }

    [Test]
    public void Limit()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            var first = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello</p>")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>world</p>")
                .Store();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello world</p>")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Id)
                .WithLimit(2);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(2, messages.Length);
            Assert.AreEqual(first.Id, messages[0].Id);
            Assert.AreEqual(second.Id, messages[1].Id);
        }
    }

    [Test]
    public void SkipAndLimit()
    {
        using (var t = _engine.NewTransaction())
        {
            var store = CreateStore(t.Handle);

            var directory = store.NewDirectory("Inbox");

            var builder = directory.CreateMessageBuilder();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello</p>")
                .Store();

            var second = builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>world</p>")
                .Store();

            builder
                .WithFrom("Alice", "alice@example.org")
                .WithTo("Bob", "bob@example.org")
                .WithSubject("test")
                .WithHtmlBody("<p>hello world</p>")
                .Store();

            var qb = new QueryBuilder()
                .WithFilter(StoredMessageFilters.DirectoryId.EqualTo(directory.Id))
                .WithOrderBy(StoredMessageFilters.Id)
                .WithSkip(1)
                .WithLimit(1);

            var messages = store.Query(qb.Build()).ToArray();

            Assert.AreEqual(1, messages.Length);
            Assert.AreEqual(second.Id, messages[0].Id);
        }
    }

    void CompareMessages(StoredMessage a, StoredMessage b)
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
    }

    Store.Store CreateStore(object handle)
    {
        var db = CreateDb();
        var store = new Store.Store(db, handle);

        return store;
    }

    protected abstract IDb CreateDb();
}