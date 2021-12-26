using Autofac;

namespace zcfux.DI;

public sealed class Container : IRegistry, IResolver
{
    readonly ContainerBuilder _builder = new ContainerBuilder();
    IContainer? _container;

    public void Register<T>(T instance) where T : class
    {
        FailIfBuilt();

        _builder.RegisterInstance(instance).As<T>();
    }

    public void Register<T>(Func<T> f) where T : class
    {
        FailIfBuilt();

        _builder.Register(_ => f()).As<T>();
    }

    public void Build()
    {
        FailIfBuilt();

        _container = _builder.Build();

        Built = true;
    }

    public bool Built { get; private set; }

    public T Resolve<T>() where T : class
    {
        FailIfNotBuilt();

        return _container!.Resolve<T>();
    }

    void FailIfBuilt()
    {
        if (Built)
        {
            throw new ContainerException("Container already built.");
        }
    }

    void FailIfNotBuilt()
    {
        if (!Built)
        {
            throw new ContainerException("Container has not been built yet.");
        }
    }
}