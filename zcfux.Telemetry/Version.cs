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
using System.Text.RegularExpressions;

namespace zcfux.Telemetry;

public static class Version
{
    static readonly Regex VersionRegex = new("^([\\d+])\\.([\\d]+)$");

    public static (int, int) Parse(string version)
    {
        var m = VersionRegex.Match(version);

        if (!m.Success)
        {
            throw new ArgumentException("Version format is invalid.");
        }

        var parts = m
            .Groups
            .Values
            .Skip(1)
            .Select(g => int.Parse(g.Value))
            .ToArray();

        return (parts[0], parts[1]);
    }

    public static bool IsCompatible(string a, string b)
    {
        var (majorA, minorA) = Parse(a);
        var (majorB, minorB) = Parse(b);
        
        return (majorA == majorB) && (minorA >= minorB);
    }
}