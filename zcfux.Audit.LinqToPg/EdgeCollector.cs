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

sealed class EdgeCollector<TEventView>
    where TEventView : IEventView
{
    public sealed record EventEdgePair(TEventView Event, EdgeView? Edge);

    public event EventHandler? Completed;

    readonly ECatalogue _catalogue;
    IEvent? _event;
    readonly List<IEdge> _edges = new();

    public EdgeCollector(ECatalogue catalogue)
        => _catalogue = catalogue;

    public IEnumerable<(IEvent, IEnumerable<IEdge>)> Collect(IQueryable<EventEdgePair> pairs)
    {
        Clear();

        foreach (var (ev, edge) in pairs)
        {
            if (_event?.Id != ev.Id)
            {
                if (_event != null)
                {
                    yield return (_event, _edges.ToArray());
                }

                _event = ToEvent(ev);

                _edges.Clear();
            }

            if (edge != null)
            {
                _edges.Add(ToEdge(edge));
            }
        }

        if (_event != null)
        {
            yield return (_event, _edges.ToArray());
        }

        Completed?.Invoke(this, EventArgs.Empty);
    }

    void Clear()
    {
        _event = null;
        _edges.Clear();
    }

    IEvent ToEvent(IEventView ev)
        => (_catalogue == ECatalogue.Recent)
            ? ev.ToRecentEvent()
            : ev.ToArchivedEvent();

    static IEdge ToEdge(EdgeView edge)
        => new Edge
        {
            Left = new Topic
            {
                Id = edge.LeftTopicId,
                Kind = new TopicKind(edge.LeftTopicKindId, edge.LeftTopicKind),
                DisplayName = edge.LeftTopic
            },
            Association = new AssociationRelation
            {
                Id = edge.AssociationId,
                Name = edge.Association
            },
            Right = new Topic
            {
                Id = edge.RightTopicId,
                Kind = new TopicKind(edge.RightTopicKindId, edge.RightTopicKind),
                DisplayName = edge.RightTopic
            }
        };
}