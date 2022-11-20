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
namespace zcfux.Audit.Test.Event;

static class Associations
{
    public static readonly Association ConnectedFrom = new(1, "connected from");
    public static readonly Association AuthenticatedAs = new(2, "authenticated as");
    public static readonly Association LoginFailed = new(3, "login failed");
    public static readonly Association CreatedSession = new(4, "created session");
    public static readonly Association Started = new(5, "started");
    public static readonly Association Stopped = new(6, "stopped");
    public static readonly Association Created = new(7, "created");
    public static readonly Association Deleted = new(8, "deleted");
};