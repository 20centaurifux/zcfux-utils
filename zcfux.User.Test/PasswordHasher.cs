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
using zcfux.User.Password;

namespace zcfux.User.Test;

sealed class PasswordHasher : AAlgorithm
{
    readonly int _format;
    readonly HashAlgorithm _hashAlgorithm;

    public PasswordHasher(int format, HashAlgorithm hashAlgorithm)
        => (_format, _hashAlgorithm) = (format, hashAlgorithm);

    public override bool Test(IPassword expected, string password)
    {
        if (_format != expected.Format)
        {
            return Next(expected, password);
        }

        var hash = ComputeHash(password, expected.Salt);

        return hash.SequenceEqual(expected.Hash);
    }

    public byte[] ComputeHash(string password, byte[] salt)
    {
        var ms = new MemoryStream();

        ms.Write(salt);
        ms.Write(password.GetBytes());

        var hash = _hashAlgorithm.ComputeHash(ms.ToArray());

        return hash;
    }
}