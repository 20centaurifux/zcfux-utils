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

namespace zcfux.DI;

public static class Extensions
{
    public static void Inject<T>(this IResolver self, T obj)
    {
        self.InjectProperties(obj);
        self.InjectFields(obj);
    }

    static void InjectProperties<T>(this IResolver self, T obj)
    {
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.CanWrite
                && self.IsRegistered(prop.PropertyType))
            {
                var value = self.Resolve(prop.PropertyType);

                prop.SetValue(obj, value);
            }
        }
    }

    static void InjectFields<T>(this IResolver self, T obj)
    {
        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.DeclaringType is { }
                && self.IsRegistered(field.FieldType))
            {
                var value = self.Resolve(field.FieldType!);

                try
                {
                    field.SetValue(obj, value);
                }
                catch
                {
                }
            }
        }
    }
}