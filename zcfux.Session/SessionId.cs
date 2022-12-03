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
using zcfux.Byte;

namespace zcfux.Session;

public sealed class SessionId
{
    readonly byte[] _id;

    public static SessionId Random()
        => Guid.NewGuid().ToByteArray();

    SessionId(byte[] id)
        => _id = id;

    public static implicit operator SessionId(byte[] id)
        => new(id);

    public static implicit operator SessionId(string idAsHex)
        => new(idAsHex.FromHex());

    public override bool Equals(object? other)
    {
        var equals = false;

        if (other is SessionId otherId)
        {
            equals = ReferenceEquals(this, other) || _id.SequenceEqual(otherId._id);
        }

        return equals;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override string ToString()
        => _id.ToHex();
}