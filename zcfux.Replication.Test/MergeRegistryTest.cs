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
using zcfux.Replication.Generic;
using zcfux.Replication.Merge;

namespace zcfux.Replication.Test
{
    public sealed class MergeAlgorithmsTests
    {
        [Test]
        public void LookupBeforeBuilt()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Register<Model>(new LastWriteWins());

            Assert.That(() => algorithms.GetGeneric<Model>(), Throws.Exception);
        }

        [Test]
        public void RegisterAfterBuilt()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Build();

            Assert.That(() => algorithms.Register<Model>(new LastWriteWins()), Throws.Exception);
        }

        [Test]
        public void BuildTwice()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Build();

            Assert.That(() => algorithms.Build(), Throws.Exception);
        }

        [Test]
        public void LookupNonExisting()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Build();

            Assert.That(() => algorithms.GetGeneric<Model>(), Throws.Exception);
        }

        [Test]
        public void LookupNonGeneric()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Register(new LastWrittenModelWins());

            algorithms.Build();

            var merge = algorithms.GetNonGeneric<Model>();

            Assert.IsInstanceOf(typeof(IMergeAlgorithm<Model>), merge);
        }

        [Test]
        public void LookupGeneric()
        {
            var algorithms = new MergeAlgorithms();

            algorithms.Register<Model>(new LastWriteWins());

            algorithms.Build();

            var merge = algorithms.GetGeneric<Model>();

            Assert.IsInstanceOf(typeof(IMergeAlgorithm), merge);
        }
    }
}