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
namespace zcfux.Security;

public static class Password
{
    public static readonly string DefaultAlgorithm = "PBKDF2";

    const int SaltSize = 8;

    static readonly Factory Factory = new();

    public static PasswordHash ComputedHash(string secret)
    {
        var salt = SecureRandom.GetBytes(SaltSize);

        return ComputedHash(DefaultAlgorithm, secret, salt);
    }
    
    public static PasswordHash ComputedHash(string algorithm, string secret)
    {
        var salt = SecureRandom.GetBytes(SaltSize);

        return ComputedHash(algorithm, secret, salt);
    }

    public static PasswordHash ComputedHash(string secret, byte[] salt)
        => ComputedHash(DefaultAlgorithm, secret, salt);

    public static PasswordHash ComputedHash(string algorithm, string secret, byte[] salt)
        => Factory.CreatePasswordAlgorithm(algorithm).ComputeHash(secret, salt);
}