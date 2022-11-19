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