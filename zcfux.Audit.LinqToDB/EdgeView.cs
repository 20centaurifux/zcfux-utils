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
namespace zcfux.Audit.LinqToDB;

#pragma warning disable CS8618
sealed class EdgeView
{
    public long EventId { get; set; }

    public long LeftTopicId { get; set; }

    public string LeftTopic { get; set; }

    public int LeftTopicKindId { get; set; }

    public string LeftTopicKind { get; set; }

    public int AssociationId { get; set; }

    public string Association { get; set; }

    public long RightTopicId { get; set; }

    public string RightTopic { get; set; }

    public int RightTopicKindId { get; set; }

    public string RightTopicKind { get; set; }

    public int? LocaleId { get; set; }

    public ILocalizedEdge ToLocalizedEdge()
        => new LocalizedEdge
        {
            Left = new LocalizedTopic
            {
                Id = LeftTopicId,
                Kind = new TopicKind(LeftTopicKindId, LeftTopicKind),
                DisplayName = LeftTopic
            },
            Association = new AssociationRelation
            {
                Id = AssociationId,
                Name = Association
            },
            Right = new LocalizedTopic
            {
                Id = RightTopicId,
                Kind = new TopicKind(RightTopicKindId, RightTopicKind),
                DisplayName = RightTopic
            }
        };
}
#pragma warning restore CS8618