namespace zcfux.DI;

public interface IRegistry
{
    void Register<T>(T instance) where T : class;

    void Register<T>(Func<T> f) where T : class;
}