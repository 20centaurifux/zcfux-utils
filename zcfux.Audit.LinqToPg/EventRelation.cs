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
[Table(Schema = "audit", Name = "Event")]
sealed class EventRelation : IEvent
{
    public EventRelation()
    {
    }

    public EventRelation(IEventKind kind, ESeverity severity, DateTime createdAt, ITopic? topic)
    {
        KindId = kind.Id;
        Kind = new EventKindRelation(kind);
        Severity = severity;
        CreatedAt = createdAt;

        if (topic != null)
        {
            TopicId = topic.Id;
            Topic = new TopicRelation(topic);
        }
    }

    [Column(Name = "Id"), PrimaryKey, Identity]
    public long Id { get; set; }

    long IEvent.Id => Id;

    [Column(Name = "KindId")]
    public int KindId { get; set; }

    [Association(ThisKey = "KindId", OtherKey = "Id")]
    public EventKindRelation Kind { get; set; }

    IEventKind IEvent.Kind => Kind;

    [Column("Severity")]
    public ESeverity Severity { get; set; }

    ESeverity IEvent.Severity => Severity;

    [Column(Name = "TopicId")]
    public long? TopicId { get; set; }

    [Association(ThisKey = "TopicId", OtherKey = "Id")]
    public TopicRelation? Topic { get; set; }

    ITopic? IEvent.Topic => Topic;

    [Column("Archived")]
    public bool Archived { get; set; }

    bool IEvent.Archived => Archived;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    DateTime IEvent.CreatedAt => CreatedAt;
}
#pragma warning restore CS8618