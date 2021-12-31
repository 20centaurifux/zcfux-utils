using System.Collections.Concurrent;
using System.Reflection;

namespace zcfux.SqlMapper;

internal static class Cache
{
    static readonly ConcurrentDictionary<Type, IDictionary<string, PropertyInfo>> Map = new();

    public static IDictionary<string, PropertyInfo> Get<T>()
    {
        var t = typeof(T);

        if (!Map.TryGetValue(t, out var m))
        {
            m = Analyzer.GetProperties<T>();

            Map[t] = m;
        }

        return m;
    }
}