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
using LinqToDB.Mapping;
using zcfux.Mail.Queue;

namespace zcfux.Mail.LinqToPg.Queue;

[Table(Schema = "mail", Name = "Queue")]
internal sealed class QueueRelation : IQueue
{
#pragma warning disable CS8618

    public QueueRelation()
    {
    }

    public QueueRelation(IQueue queue)
    {
        Id = queue.Id;
        Name = queue.Name;
    }

    [Column(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
    public int? Id { get; set; }

    int IQueue.Id => Id!.Value;

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    string IQueue.Name => Name;
#pragma warning restore CS8618
}