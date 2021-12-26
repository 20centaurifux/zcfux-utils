using System.Reflection;

namespace zcfux.Logging;

internal static class TypeLoader
{
    public static IDictionary<string, Type> GetTypes()
    {
        var writers = GetLoadedWriters();

        return writers.ToDictionary(t => GetLoggerAttribute(t).Name, t => t);
    }

    static IEnumerable<Type> GetLoadedWriters()
        => GetLoadedTypes().Where(t => typeof(IWriter).IsAssignableFrom(t)
                                       && t.IsClass
                                       && !t.IsAbstract);

    static IEnumerable<Type> GetLoadedTypes()
        => GetAssemblies().SelectMany(asm => asm.GetTypes());

    static IEnumerable<Assembly> GetAssemblies()
        => AppDomain.CurrentDomain.GetAssemblies();

    static LoggerAttribute GetLoggerAttribute(ICustomAttributeProvider type)
        => type.GetCustomAttributes(typeof(LoggerAttribute), true)
            .OfType<LoggerAttribute>()
            .Single();
}