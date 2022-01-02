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

public static class Bcd
{
    public static void ParseAscii(string ascii, out byte[] dst)
    {
        dst = new byte[ascii.Length / 2];

        for (var i = 0; i < ascii.Length; i++)
        {
            var c = ascii[i];

            if (c is < '0' or > '9')
            {
                throw new ArgumentException($"Unexpected character: '{c}'");
            }

            var n = (byte)(c - 0x30);

            if (i % 2 == 0)
            {
                dst[i / 2] = (byte)((n << 4) & 0xf0);
            }
            else
            {
                dst[i / 2] |= (byte)(n & 0x0f);
            }
        }
    }

    public static string ToAscii(IEnumerable<byte> bytes)
    {
        var sb = new StringBuilder();

        foreach (var b in bytes)
        {
            var first = ((b >> 4) & 0x0f) + 0x30;

            sb.Append((char)first);

            var second = (b & 0xf) + 0x30;

            sb.Append((char)second);
        }

        return sb.ToString();
    }

    public static long ToIntegral(IEnumerable<byte> bytes, int offset, int count)
        => ToIntegral(bytes.Skip(offset)
            .Take(count)
            .ToArray());

    public static long ToIntegral(byte[] bytes)
    {
        long n = 0;

        var digit = 1;

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[bytes.Length - 1 - i];

            var second = b & 0x0f;

            n += (second * digit);

            digit *= 10;

            var first = (b >> 4) & 0x0f;

            n += (first * digit);

            digit *= 10;
        }

        return n;
    }
}