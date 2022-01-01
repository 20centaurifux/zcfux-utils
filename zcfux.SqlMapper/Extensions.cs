/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Data;

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