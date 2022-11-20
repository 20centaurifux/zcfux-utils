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
using System.Reflection;

namespace zcfux.SqlMapper;

static class Analyzer
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