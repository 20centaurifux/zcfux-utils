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
    public static void Inject(this IResolver self, object obj)
    {
        self.InjectProperties(obj);
        self.InjectFields(obj);
    }

    static void InjectProperties(this IResolver self, object obj)
    {
        foreach (var prop in GetProperties(obj))
        {
            if (prop.CanWrite
                && self.IsRegistered(prop.PropertyType))
            {
                var value = self.Resolve(prop.PropertyType);

                prop.SetValue(obj, value);
            }
        }
    }

    static IEnumerable<PropertyInfo> GetProperties(object obj)
    {
        Type? type = obj.GetType();

        while (type is { })
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null))
            {
                yield return property;
            }

            type = type.BaseType;
        }
    }

    static void InjectFields(this IResolver self, object obj)
    {
        foreach (var field in GetFields(obj))
        {
            if (field.FieldType is { }
                && self.IsRegistered(field.FieldType))
            {
                var value = self.Resolve(field.FieldType);

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

    static IEnumerable<FieldInfo> GetFields(object obj)
    {
        Type? type = obj.GetType();

        while (type is { })
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null))
            {
                yield return field;
            }

            type = type.BaseType;
        }
    }


    public static T Inject<T>(this IResolver self)
        where T : class
    {
        var ctor = self.FindConstructor<T>()
            ?? throw new ContainerException("No compatible constructor found.");

        var parameters = self.CreateParameters(ctor).ToArray();

        var instance = ctor.Invoke(parameters) as T;

        return instance!;
    }

    static ConstructorInfo? FindConstructor<T>(this IResolver self)
    {
        var ctors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        var match = ctors
            .SingleOrDefault(ctor =>
                ctor
                    .GetParameters()
                    .Select(p => p.ParameterType)
                    .All(t => self.IsRegistered(t)));

        return match;
    }

    static IEnumerable<object> CreateParameters(this IResolver self, ConstructorInfo ctor)
    {
        foreach (var parameter in ctor.GetParameters())
        {
            yield return self.Resolve(parameter.ParameterType);
        }
    }
}