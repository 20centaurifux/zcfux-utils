using System.Data;
using System.Net.Mail;

namespace zcfux.SqlMapper;

public static class Extensions
{
    public static T ToObject<T>(this IDataReader self) where T : new()
    {
        var obj = new T();

        var m = Cache.Get<T>();

        foreach (var (field, prop) in m)
        {
            var ordinal = self.GetOrdinal(field);
            var value = self.GetValue(ordinal);

            if (Convert.IsDBNull(value))
            {
                value = null;
            }

            prop.SetValue(obj, value);
        }

        return obj;
    }

    public static T? ReadAndMap<T>(this IDataReader self) where T : new()
    {
        var obj = default(T);

        if (self.Read())
        {
            obj = self.ToObject<T>();
        }

        return obj;
    }

    public static T? FetchOne<T>(this IDbCommand self) where T : new()
    {
        using (var reader = self.ExecuteReader())
        {
            return ReadAndMap<T>(reader);
        }
    }

    public static IEnumerable<T> FetchLazy<T>(this IDbCommand self) where T : new()
    {
        var reader = self.ExecuteReader();

        return new LazyDataReader<T>(reader);
    }

    public static void Assign<T>(this IDbCommand self, T obj)
        => self.Assign(obj, prop => $"@{prop}");

    public static void Assign<T>(this IDbCommand self, T obj, Func<string, string> propToKey)
    {
        var m = Cache.Get<T>();

        foreach (var (name, prop) in m)
        {
            var key = propToKey(name);

            if (self.CommandText.Contains(key))
            {
                var paramKey = $"@{name}";

                if (paramKey != key)
                {
                    self.CommandText = self.CommandText.Replace(key, paramKey);
                }

                var param = self.CreateParameter();

                param.ParameterName = paramKey;
                param.Value = prop.GetValue(obj) ?? DBNull.Value;

                self.Parameters.Add(param);
            }
        }
    }
}