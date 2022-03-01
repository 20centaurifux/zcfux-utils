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
using System.Collections.Concurrent;

namespace zcfux.Replication.CouchDb;

internal static class TypeMap
{
    static readonly ConcurrentDictionary<string, Type> Types = new ConcurrentDictionary<string, Type>();

    static readonly Lazy<Type[]> AvailableTypes = new(() =>
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .ToArray();
    });

    public static Type Get(string fullname)
    {
        if (!Types.TryGetValue(fullname, out var type))
        {
            type = AvailableTypes
                .Value
                .FirstOrDefault(t => t.FullName == fullname);

            Types[fullname] = type ?? throw new ArgumentException($"Type `{fullname}' not found.");
        }

        return type;
    }
}