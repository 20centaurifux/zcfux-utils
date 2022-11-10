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
using System.Reflection;
using NUnit.Framework;

namespace zcfux.Logging.Test;

public sealed class LoggingTests
{
    [SetUp]
    public void Setup()
        => Writer.Collect.Messages.Clear();

    [Test]
    public void Trace()
        => Test(ESeverity.Trace);

    [Test]
    public void Debug()
        => Test(ESeverity.Debug);

    [Test]
    public void Info()
        => Test(ESeverity.Info);

    [Test]
    public void Warn()
        => Test(ESeverity.Warn);

    [Test]
    public void Error()
        => Test(ESeverity.Error);

    [Test]
    public void Fatal()
        => Test(ESeverity.Fatal);

    [Test]
    public void Verbosity()
    {
        var logger = Factory.Instance.FromName("collect");

        logger.Trace(RandomString());
        logger.Debug(RandomString());

        Assert.IsFalse(Writer.Collect.Messages.ContainsKey(ESeverity.Trace));
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Debug].Count);

        logger.Verbosity = ESeverity.Info;
        
        logger.Debug(RandomString());
        logger.Info(RandomString());
        
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Debug].Count);
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Info].Count);
        
        logger.Verbosity = ESeverity.Warn;
        
        logger.Info(RandomString());
        logger.Warn(RandomString());

        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Info].Count);
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Warn].Count);
        
        logger.Verbosity = ESeverity.Error;
        
        logger.Warn(RandomString());
        logger.Error(RandomString());

        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Warn].Count);
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Error].Count);
        
        logger.Verbosity = ESeverity.Fatal;
        
        logger.Error(RandomString());
        logger.Fatal(RandomString());

        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Error].Count);
        Assert.AreEqual(1, Writer.Collect.Messages[ESeverity.Fatal].Count);
    }

    static void Test(ESeverity severity)
    {
        var logger = Factory.Instance.FromName("collect");

        logger.Verbosity = ESeverity.Trace;

        var methodName = severity.ToString();

        var fn1 = logger.GetType()
            .GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public,
                new[] { typeof(string) });

        fn1?.Invoke(logger,
            new object[] { RandomString() });

        var fn2 = logger.GetType()
            .GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public,
                new[] { typeof(string), typeof(object[]) });

        fn2?.Invoke(logger,
            new object[] { "{0} {1}", new object[] { RandomString(), RandomLong() } });

        fn2?.Invoke(logger,
            new object[] { "{0} {1} {2}", new object[] { RandomString(), RandomLong() } });

        var fn3 = logger.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public,
                new[] { typeof(Exception) });

        fn3?.Invoke(logger,
            new object[] { new Exception(RandomString()) });

        foreach (var s in Enum.GetValues(typeof(ESeverity)).Cast<ESeverity>())
        {
            if (s == severity)
            {
                Assert.AreEqual(3, Writer.Collect.Messages[s].Count);
            }
            else
            {
                Assert.IsFalse(Writer.Collect.Messages.ContainsKey(s));
            }
        }
    }

    static string RandomString()
        => TestContext.CurrentContext.Random.GetString();

    static long RandomLong()
        => TestContext.CurrentContext.Random.NextLong();
}