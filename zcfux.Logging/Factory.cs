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
namespace zcfux.Logging;

public static class Factory
{
    static readonly Lazy<IDictionary<string, Type>> Types = new(TypeLoader.GetTypes());

    public static ILogger ByName(string name)
    {
        var writer = CreateWriter(name);

        return new BasicLogger(writer);
    }

    public static ILogger ByName(params string[] names)
    {
        var chain = new Chain();

        foreach (var name in names)
        {
            chain = chain.Append(CreateWriter(name));
        }

        return chain;
    }

    public static ILogger FromAssembly(string assemblyName, string typeName)
    {
        try
        {
            var writer = Activator.CreateInstance(assemblyName, typeName)!.Unwrap();

            return new BasicLogger((writer as IWriter)!);
        }
        catch (Exception ex)
        {
            throw new FactoryException("Couldn't create writer.", ex);
        }
    }

    public static ILogger FromAssembly(params (string, string)[] tuples)
    {
        var chain = new Chain();

        try
        {
            foreach (var (assemblyName, typeName) in tuples)
            {
                var writer = Activator.CreateInstance(assemblyName, typeName)!.Unwrap();

                chain = chain.Append((writer as IWriter)!);
            }

            return chain;
        }
        catch (Exception ex)
        {
            throw new FactoryException("Couldn't create writer.", ex);
        }
    }

    static IWriter CreateWriter(string name)
    {
        IWriter? writer = null;

        if (Types.Value.TryGetValue(name, out var t))
        {
            writer = Activator.CreateInstance(t) as IWriter;
        }

        return writer ?? throw new FactoryException($"Writer `{name}' not found.");
    }
}