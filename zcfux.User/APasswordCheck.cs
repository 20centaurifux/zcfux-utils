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
namespace zcfux.User;

public abstract class APasswordCheck
{
    APasswordCheck? _next;

    public APasswordCheck Concat(APasswordCheck other)
    {
        other._next = this;

        return other;
    }

    public bool Check(IUser user, string password)
        => CanCheckPassword(user)
            ? PasswordIsCorrect(user, password)
            : Next(user, password);

    protected abstract bool CanCheckPassword(IUser user);

    protected abstract bool PasswordIsCorrect(IUser user, string password);

    bool Next(IUser user, string password)
        => (_next != null)
            && _next.Check(user, password);
}