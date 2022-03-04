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
using zcfux.Replication.Merge;

namespace zcfux.Replication.Test
{
    public abstract class AReaderTests
    {
        protected const string Side = "a";

        [SetUp]
        public void Setup()
            => CreateDb();

        [TearDown]
        public void TearDown()
            => DropDb();

        protected abstract void CreateDb();

        protected abstract void DropDb();

        [Test]
        public void CreateNewReader()
        {
            var reader = CreateReader();

            Assert.IsInstanceOf<AReader>(reader);
            Assert.AreEqual(Side, reader.Side);
        }

        [Test]
        public void ReadNonExistingEntity()
        {
            var reader = CreateReader();

            Assert.That(() => reader.Read<Model>(Guid.NewGuid()), Throws.Exception);
        }

        [Test]
        public void ReadEntity()
        {
            var model = new Model
            {
                Guid = Guid.NewGuid(),
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var writer = CreateWriter();

            var result = writer.TryCreate(model, DateTime.UtcNow, out var a);

            Assert.AreEqual(ECreateResult.Success, result);

            model = new Model
            {
                Guid = model.Guid,
                Text = TestContext.CurrentContext.Random.GetString()
            };

            var b = writer.Update(model, a!.Revision!, DateTime.UtcNow, new LastWriteWins());

            var reader = CreateReader();

            var c = reader.Read<Model>(model.Guid);

            Assert.AreNotEqual(a.Entity, c.Entity);
            Assert.AreEqual(a.IsNew, c.IsNew);
            Assert.AreNotEqual(a.Modified, c.Modified);
            Assert.AreNotEqual(a.Revision, c.Revision);
            Assert.AreEqual(a.Side, c.Side);

            Assert.AreEqual(b.Entity, c.Entity);
            Assert.AreEqual(b.IsNew, c.IsNew);
            Assert.AreEqual(b.Modified, c.Modified);
            Assert.AreEqual(b.Revision, c.Revision);
            Assert.AreEqual(b.Side, c.Side);
        }

        protected abstract AReader CreateReader();

        protected abstract AWriter CreateWriter();
    }
}