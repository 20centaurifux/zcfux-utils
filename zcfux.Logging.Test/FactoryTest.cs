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

namespace zcfux.Logging.Test;

public sealed class FactoryTest
{
    [Test]
    public void CreateLoggerByName()
    {
        var logger = Factory.ByName("void");

        Assert.IsInstanceOf<ILogger>(logger);
    }

    [Test]
    public void CreateLoggerByUnknownName()
    {
        var loggerName = TestContext.CurrentContext.Random.GetString();

        Assert.Throws<FactoryException>(() => Factory.ByName(loggerName));
    }

    [Test]
    public void CreateLoggerFromAssembly()
    {
        var logger = Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer");

        Assert.IsInstanceOf<ILogger>(logger);
    }

    [Test]
    public void CreateLoggerFromUnknownAssembly()
    {
        var assemblyName = TestContext.CurrentContext.Random.GetString();

        Assert.Throws<FactoryException>(() => Factory.FromAssembly(assemblyName, "zcfux.Logging.Console.Writer"));
    }

    [Test]
    public void CreateUnknownLoggerFromAssembly()
    {
        var writerName = TestContext.CurrentContext.Random.GetString();

        Assert.Throws<FactoryException>(() => Factory.FromAssembly("zcfux.Logging", writerName));
    }
}