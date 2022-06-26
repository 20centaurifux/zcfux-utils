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
using System.Security.Cryptography;

namespace zcfux.KeyValueStore.Persistent;

internal sealed class CopyUtil
{
    byte[]? _hash;
    long _length;

    public void Copy(Stream from, Stream to)
    {
        var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[8192];

        _length = 0;

        var read = from.Read(buffer, 0, buffer.Length);

        while (read > 0)
        {
            sha256.AppendData(buffer, 0, read);
            to.Write(buffer, 0, read);

            _length += read;
            
            read = from.Read(buffer, 0, buffer.Length);
        }

        _hash = sha256.GetCurrentHash();
    }

    public byte[]? Hash => _hash;
    
    public long Length => _length;
}