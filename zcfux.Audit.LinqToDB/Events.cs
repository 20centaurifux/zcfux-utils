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
using LinqToDB;
using zcfux.Data.LinqToDB;

namespace zcfux.Audit.LinqToDB;

sealed class Events : IEvents
{
    public void InsertEventKind(object handle, IEventKind kind)
        => handle.Db().Insert(new EventKindRelation(kind));

    public IEventKind GetEventKind(object handle, int id)
        => handle.Db()
            .GetTable<EventKindRelation>()
            .Single(kind => kind.Id == id);

    public IEvent NewEvent(object handle, IEventKind kind, ESeverity severity, DateTime createdAt, ITopic? topic)
    {
        var ev = new EventRelation(kind, severity, createdAt, topic);

        ev.Id = Convert.ToInt64(handle.Db().InsertWithIdentity(ev));

        return ev;
    }

    public ICatalogue CreateCatalogue(object handle, ECatalogue catalogue)
        => new Catalogue(handle, catalogue);

    public void ArchiveEvents(object handle, DateTime before)
        => handle.Db()
            .GetTable<EventRelation>()
            .Where(ev => ev.CreatedAt <= before)
            .Set(ev => ev.Archived, true)
            .Update();
}