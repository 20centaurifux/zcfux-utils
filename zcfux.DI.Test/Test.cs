using NUnit.Framework;

namespace zcfux.DI.Test;

public sealed class Test
{
    [Test]
    public void BuildContainer()
    {
        var container = new Container();

        Assert.IsFalse(container.Built);

        container.Build();

        Assert.IsTrue(container.Built);
    }

    [Test]
    public void BuildTwice()
    {
        var container = new Container();

        container.Build();

        Assert.Throws<ContainerException>(() => container.Build());
    }

    [Test]
    public void ResolveBeforeBuild()
    {
        var container = new Container();

        Assert.Throws<ContainerException>(() => container.Resolve<string>());
    }

    [Test]
    public void RegisterAfterBuild()
    {
        var container = new Container();

        container.Build();

        Assert.Throws<ContainerException>(() => container.Register(string.Empty));
    }

    [Test]
    public void RegisterAndResolveClass()
    {
        var container = new Container();

        var a = TestContext.CurrentContext.Random.GetString();

        container.Register(a);

        container.Build();

        var b = container.Resolve<string>();

        Assert.AreSame(a, b);
    }

    [Test]
    public void RegisterAndResolveFunction()
    {
        var container = new Container();

        container.Register(() => TestContext.CurrentContext.Random.GetString());

        container.Build();

        var a = container.Resolve<string>();
        var b = container.Resolve<string>();

        Assert.AreNotSame(a, b);
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void ResolveUnknown()
    {
        var container = new Container();

        container.Build();

        Assert.That(() => container.Resolve<string>(), Throws.Exception);
    }
}