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

namespace zcfux.Audit.LinqToDB;

#pragma warning disable CS8618
[Table(Schema = "audit", Name = "Topic")]
internal sealed class TopicRelation : ITopic
{
    public TopicRelation()
    {
    }

    public TopicRelation(ITopicKind kind, string displayName)
    {
        KindId = kind.Id;
        Kind = new TopicKindRelation(kind);
        DisplayName = displayName;
    }

    public TopicRelation(ITopic topic)
    {
        Id = topic.Id;
        KindId = topic.Kind.Id;
        Kind = new TopicKindRelation(topic.Kind);
        DisplayName = topic.DisplayName;
    }

    [Column(Name = "Id"), PrimaryKey, Identity]
    public long Id { get; set; }

    long ITopic.Id => Id;

    [Column(Name = "KindId")]
    public int KindId { get; set; }

    [Association(ThisKey = "KindId", OtherKey = "Id")]
    public TopicKindRelation Kind { get; set; }

    ITopicKind ITopic.Kind => Kind;

    [Column(Name = "DisplayName")]
    public string DisplayName { get; set; }

    string ITopic.DisplayName => DisplayName;
}
#pragma warning restore CS8618