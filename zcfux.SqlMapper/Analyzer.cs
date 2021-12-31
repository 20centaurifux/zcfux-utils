using System.Reflection;

namespace zcfux.SqlMapper;

internal static class Analyzer
{
    public static IDictionary<string, PropertyInfo> GetProperties<T>()
        => IsModel<T>()
            ? GetAnnotatedColumns<T>()
            : GetAllProperties<T>();

    static bool IsModel<T>()
        => Attribute.GetCustomAttribute(typeof(T), typeof(ModelAttribute)) != null;

    static IDictionary<string, PropertyInfo> GetAnnotatedColumns<T>()
    {
        var m = typeof(T).GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(ColumnAttribute)))
            .ToDictionary(prop =>
            {
                var attr = prop.GetCustomAttribute(typeof(ColumnAttribute), false);

                return (attr as ColumnAttribute)!.ColumnName;
            });

        return m;
    }

    static IDictionary<string, PropertyInfo> GetAllProperties<T>()
    {
        var m = typeof(T).GetProperties()
            .ToDictionary(prop => prop.Name);

        return m;
    }
}