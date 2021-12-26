namespace zcfux.DI;

public interface IResolver
{
    T Resolve<T>() where T : class;
}