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
namespace zcfux.Audit.LinqToPg;

interface IEventView
{
    long Id { get; set; }

    int KindId { get; set; }

    string Kind { get; set; }

    ESeverity Severity { get; set; }

    DateTime CreatedAt { get; set; }

    long? TopicId { get; set; }

    string? DisplayName { get; set; }
    
    int? LocaleId { get; set; }
}

static class EventViewExtensions
{
    public static ILocalizedEvent ToRecentLocalizedEvent(this IEventView self)
        => self.ToLocalizedEvent(archived: false);

    public static ILocalizedEvent ToArchivedLocalizedEvent(this IEventView self)
        => self.ToLocalizedEvent(archived: true);

    public static ILocalizedEvent ToLocalizedEvent(this IEventView self, bool archived)
    {
        var ev = new LocalizedEvent
        {
            Id = self.Id,
            Kind = new EventKindRelation
            {
                Id = self.KindId,
                Name = self.Kind
            },
            CreatedAt = self.CreatedAt,
            Severity = self.Severity,
            Archived = archived
        };

        if (self.TopicId.HasValue)
        {
            ev.Topic = new LocalizedTopic
            {
                Id = self.TopicId.Value,
                DisplayName = self.DisplayName!
            };
        }

        return ev;
    }
}