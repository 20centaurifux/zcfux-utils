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

namespace zcfux.Audit.LinqToPg;

#pragma warning disable CS8618
class EventView : IEventView
{
    [Column(Name = "Id")]
    public long Id { get; set; }

    [Column(Name = "KindId")]
    public int KindId { get; set; }

    [Column(Name = "Kind")]
    public string Kind { get; set; }

    [Column(Name = "Severity")]
    public ESeverity Severity { get; set; }

    [Column(Name = "CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "TopicId")]
    public long? TopicId { get; set; }

    [Column(Name = "DisplayName")]
    public string? DisplayName { get; set; }
}
#pragma warning restore CS8618