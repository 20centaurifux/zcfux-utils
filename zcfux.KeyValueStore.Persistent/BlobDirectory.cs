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
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace zcfux.KeyValueStore.Persistent;

internal sealed class BlobDirectory
{
    static readonly Regex HashRegex = new("[0-9A-F]{64}$");
    readonly string _path;

    public BlobDirectory(string path)
        => _path = path;

    public IEnumerable<(string, string)> FindAll()
    {
        var directory = new DirectoryInfo(_path);

        return Search(directory, ImmutableList.Create<string>());
    }

    static IEnumerable<(string, string)> Search(DirectoryInfo directory, ImmutableList<string> parts)
    {
        var subdirectories = directory.GetDirectories();

        foreach (var subdirectory in subdirectories)
        {
            foreach (var hex in Search(subdirectory, parts.Add(subdirectory.Name)))
            {
                yield return hex;
            }
        }

        foreach (var file in directory.GetFiles())
        {
            parts = parts.Add(file.Name);

            var hex = string.Join(string.Empty, parts);

            if (HashRegex.IsMatch(hex))
            {
                yield return (hex, file.FullName);
            }
        }
    }
}