using NUnit.Framework;

namespace zcfux.Logging.Test;

public sealed class FactoryTest
{
    [Test]
    public void CreateLogger()
    {
        var logger = Factory.ByName("void");

        Assert.IsInstanceOf<ILogger>(logger);
    }

    [Test]
    public void CreateUnknownLogger()
    {
        var loggerName = TestContext.CurrentContext.Random.GetString();

        Assert.Throws<FactoryException>(() => Factory.ByName(loggerName));
    }
}