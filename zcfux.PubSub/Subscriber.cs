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
namespace zcfux.PubSub;

public sealed class Subscriber
{
    public Subscriber(string id)
        => Id = id;

    public static implicit operator Subscriber(string id)
        => new(id);

    public string Id { get; }

    public override bool Equals(object? other)
    {
        var equals = false;

        if (other is Subscriber otherSubscriber)
        {
            equals = ReferenceEquals(this, other) || Id.Equals(otherSubscriber.Id);
        }

        return equals;
    }

    public override int GetHashCode()
        => Id.GetHashCode();

    public override string ToString()
        => Id;
}