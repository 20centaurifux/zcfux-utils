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
using zcfux.CredentialStore.Memory;

namespace zcfux.CredentialStore.Test;

public sealed class CachingReaderWriterTests : AReaderWriterTests
{
    protected override IStore CreateAndSetupStore()
    {
        var memoryStore = new Store();

        memoryStore.Setup();

        var builder = new Builder()
            .WithCachingReader(
                memoryStore.CreateReader(),
                new CachingReader.Options(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.Zero))
            .WithWriter(memoryStore.CreateWriter());

        var store = builder.Build();

        store.Setup();

        return store;
    }

    [Test]
    public void ReadSecretFromCache()
    {
        var store = CreateAndSetupStore();

        var writer = store.CreateWriter();

        var firstVersion = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", firstVersion);

        var reader = store.CreateReader();

        reader.Read("/user");

        var secondVersion = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", secondVersion);

        var received = reader.Read("/user");

        Assert.AreEqual(firstVersion, received);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        received = reader.Read("/user");

        Assert.AreEqual(secondVersion, received);
    }

    [Test]
    public void ReadRemovedSecretFromCache()
    {
        var store = CreateAndSetupStore();

        var writer = store.CreateWriter();

        var secret = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .Build();

        writer.Write("/user", secret);

        var reader = store.CreateReader();

        reader.Read("/user");

        writer.Remove("/user");

        var received = reader.Read("/user");

        Assert.AreEqual(received, received);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
    }

    [Test]
    public void CacheValidAfterGarbageCollection()
    {
        var store = CreateAndSetupStore();

        var writer = store.CreateWriter();

        var secret = new SecretBuilder()
            .WithData("password", TestContext.CurrentContext.Random.GetString())
            .WithExpiryDate(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1)))
            .Build();

        writer.Write("/user", secret);

        var reader = (store.CreateReader() as CachingReader)!;

        reader.Read("/user");

        reader.CollectGarbage();

        var received = reader.Read("/user");

        Assert.AreEqual(secret, received);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        reader.CollectGarbage();

        Assert.Throws<SecretNotFoundException>(() => reader.Read("/user"));
    }
}