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
using System.Text;

namespace zcfux.Byte;

public static class Extensions
{
    public static byte[] GetBytes(this string self)
        => Encoding.UTF8.GetBytes(self);

    public static string ToHex(this IEnumerable<byte> self)
    {
        var array = self.ToArray();

        var sb = new StringBuilder(array.Length * 2);

        foreach (var b in array)
        {
            sb.AppendFormat("{0:X2}", b);
        }

        return sb.ToString();
    }

    public static byte[] FromHex(this string self)
    {
        if ((self.Length % 2) > 0)
        {
            throw new ArgumentException("String has invalid length.");
        }

        var bytes = new byte[self.Length / 2];

        for (var i = 0; i < self.Length; i += 2)
        {
            var substr = self.Substring(i, 2);

            bytes[i / 2] = byte.Parse(substr, System.Globalization.NumberStyles.HexNumber);
        }

        return bytes;
    }
}