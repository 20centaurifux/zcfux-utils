using System.Reflection;
using NUnit.Framework;

namespace zcfux.Logging.Test;

public sealed class LoggingTest
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

    static void Test(ESeverity severity)
    {
        var logger = Factory.ByName("collect");

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