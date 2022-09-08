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

public abstract class APasswordChanger
{
    APasswordChanger? _next;

    public APasswordChanger Concat(APasswordChanger other)
    {
        other._next = this;

        return other;
    }

    public void Change(IUser user, string password)
    {
        if (CanChangePassword(user))
        {
            ChangePassword(user, password);
        }
        else
        {
            Next(user, password);
        }
    }

    public bool IsChangable(IUser user)
        => CanChangePassword(user)
            || (_next != null && _next.IsChangable(user));

    protected abstract bool CanChangePassword(IUser user);

    protected abstract void ChangePassword(IUser user, string password);

    void Next(IUser user, string password)
    {
        if (_next == null)
        {
            throw new InvalidOperationException("Password not changable.");
        }

        _next.Change(user, password);
    }
}