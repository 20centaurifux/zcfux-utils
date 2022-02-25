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
namespace zcfux.Replication.Generic;

public class Version<T> : IVersion<T>, IVersion
    where T : IEntity
{
    public Version(T entity, string revision, string side, DateTime modified)
        => (Entity, Revision, Side, Modified) = (entity, revision, side, modified);

    public T Entity { get; }

    IEntity IVersion.Entity => Entity;

    public string Revision { get; }

    string IVersion.Revision => Revision;

    public string Side { get; }

    string IVersion.Side => Side;

    public DateTime Modified { get; }

    DateTime IVersion.Modified => Modified;

    public bool IsNew => string.IsNullOrEmpty(Revision);

    bool IVersion.IsNew => IsNew;
}