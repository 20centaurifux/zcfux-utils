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
using zcfux.Byte;

namespace zcfux.Security;

public sealed class MessageAuthenticator
{
    public static readonly string DefaultAlgorithm = "HMAC-SHA-256";

    readonly Factory _factory = new();
    readonly string _algorithm = DefaultAlgorithm;
    readonly byte[] _secret;

    public MessageAuthenticator(string secret)
        => _secret = secret.GetBytes();

    public MessageAuthenticator(string algorithm, string secret)
        => (_algorithm, _secret) = (algorithm, secret.GetBytes());

    public Hash ComputeHash(string message)
        => ComputeHash(message.GetBytes());

    public Hash ComputeHash(byte[] bytes)
    {
        var digest = NewAlgorithm().ComputeHash(bytes);

        return new Hash(_algorithm, digest);
    }

    KeyedHashAlgorithm NewAlgorithm()
        => _factory.CreateKeyedHashAlgorithm(_algorithm, _secret);

    public bool Verify(byte[] message, byte[] checksum)
        => checksum.SequenceEqual(ComputeHash(message).Digest);

    public bool Verify(string message, byte[] checksum)
        => checksum.SequenceEqual(ComputeHash(message).Digest);
}