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

internal sealed class Associations : IAssociations
{
    public void InsertAssociation(object handle, IAssociation association)
        => handle.Db()
            .Insert(new AssociationRelation(association));

    public IAssociation GetAssociation(object handle, long id)
        => handle.Db()
            .GetTable<AssociationRelation>()
            .Single(association => association.Id == id);

    public IEdge Associate(object handle, ITopic first, IAssociation association, ITopic second)
    {
        var topicAssociation = new TopicAssociationRelation
        {
            Topic1 = first.Id,
            AssociationId = association.Id,
            Topic2 = second.Id
        };

        handle.Db().Insert(topicAssociation);

        var edge = new Edge
        {
            Left = first,
            Association = association,
            Right = second
        };

        return edge;
    }
}