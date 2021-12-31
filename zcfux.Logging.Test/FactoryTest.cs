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