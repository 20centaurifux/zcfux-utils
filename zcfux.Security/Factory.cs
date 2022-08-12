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

namespace zcfux.Security;

public sealed class Factory
{
    public HashAlgorithm CreateHashAlgorithm(string name)
    {
        return name switch
        {
            "SHA-256" => SHA256.Create(),
            "SHA-384" => SHA384.Create(),
            "SHA-512" => SHA512.Create(),
            _ => throw new UnsupportedAlgorithmException()
        };
    }

    public KeyedHashAlgorithm CreateKeyedHashAlgorithm(string name, byte[] secret)
    {
        return name switch
        {
            "HMAC-SHA-256" => new HMACSHA256(secret),
            "HMAC-SHA-384" => new HMACSHA384(secret),
            "HMAC-SHA-512" => new HMACSHA512(secret),
            _ => throw new UnsupportedAlgorithmException()
        };
    }

    public IPasswordHashAlgorithm CreatePasswordAlgorithm(string name)
    {
        if (name == "PBKDF2")
        {
            return new Pbkdf2();
        }

        throw new UnsupportedAlgorithmException();
    }
}