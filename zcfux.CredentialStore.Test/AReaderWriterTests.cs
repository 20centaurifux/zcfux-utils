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

namespace zcfux.CredentialStore.Test;

public abstract class AReaderWriterTests
{
    IStore _store = null!;

    [SetUp]
    public void Setup()
        => _store = CreateAndSetupStore();

    [Test]
    public void IsReadable()
    {
        Assert.IsTrue(_store.CanRead);

        var reader = _store.CreateReader();

        Assert.IsInstanceOf<IReader>(reader);
    }

    [Test]
    public void IsWritable()
    {
        Assert.IsTrue(_store.CanWrite);

        var writer = _store.CreateWriter();

        Assert.IsInstanceOf<IWriter>(writer);
    }

    [Test]
    public void WriteAndReadSecret()
    {
        var writer = _store.CreateWriter();

        var secret = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", secret);

        var reader = _store.CreateReader();

        var received = reader.Read("/user");

        Assert.AreEqual(secret, received);
    }

    [Test]
    public void OverwriteAndReadSecret()
    {
        var writer = _store.CreateWriter();

        var firstVersion = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", firstVersion);

        var secondVersion = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", secondVersion);

        var reader = _store.CreateReader();

        var received = reader.Read("/user");

        Assert.AreNotEqual(firstVersion, received);
        Assert.AreEqual(secondVersion, received);
    }

    [Test]
    public void WriteAndReadExpiringSecret()
    {
        var writer = _store.CreateWriter();

        var secret = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .WithExpiryDate(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1)))
            .Build();

        writer.Write("/user", secret);

        var reader = _store.CreateReader();

        var received = reader.Read("/user");

        Assert.AreEqual(secret, received);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
    }

    [Test]
    public void ReadNonExistingSecret()
    {
        var reader = _store.CreateReader();

        Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
    }

    [Test]
    public void WriteAndDeleteAndReadSecret()
    {
        var secret = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        var writer = _store.CreateWriter();

        writer.Write("/user", secret);
        writer.Remove("/user");

        var reader = _store.CreateReader();

        Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
    }

    [Test]
    public void ValidAfterGarbageCollection()
    {
        if (_store is ICollectGarbage gc)
        {
            var secret = new SecretBuilder()
                .WithData("password", TestContext.CurrentContext.Random.GetString())
                .WithExpiryDate(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1)))
                .Build();

            var writer = _store.CreateWriter();

            writer.Write("/user", secret);

            var reader = _store.CreateReader();

            gc.CollectGarbage();

            var received = reader.Read("/user");

            Assert.AreEqual(received, secret);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            gc.CollectGarbage();

            Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
        }
    }

    protected abstract IStore CreateAndSetupStore();
}