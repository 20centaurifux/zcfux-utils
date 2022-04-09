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
namespace zcfux.Audit.Test.Event;

internal sealed class Utility
{
    readonly IAuditDb _db;
    readonly object _handle;

    public Utility(IAuditDb db, object handle)
        => (_db, _handle) = (db, handle);

    public void InsertDefaultEntries()
    {
        _db.Events.InsertEventKind(_handle, EventKinds.Security);
        _db.Events.InsertEventKind(_handle, EventKinds.Service);

        _db.Topics.InsertTopicKind(_handle, TopicKinds.Event);
        _db.Topics.InsertTopicKind(_handle, TopicKinds.Endpoint);
        _db.Topics.InsertTopicKind(_handle, TopicKinds.User);
        _db.Topics.InsertTopicKind(_handle, TopicKinds.Session);
        _db.Topics.InsertTopicKind(_handle, TopicKinds.Service);

        _db.Associations.InsertAssociation(_handle, Associations.ConnectedFrom);
        _db.Associations.InsertAssociation(_handle, Associations.AuthenticatedAs);
        _db.Associations.InsertAssociation(_handle, Associations.LoginFailed);
        _db.Associations.InsertAssociation(_handle, Associations.CreatedSession);
        _db.Associations.InsertAssociation(_handle, Associations.Started);
        _db.Associations.InsertAssociation(_handle, Associations.Stopped);
        _db.Associations.InsertAssociation(_handle, Associations.Created);
        _db.Associations.InsertAssociation(_handle, Associations.Deleted);
    }

    public IEvent InsertLoginEvent(DateTime at, string endpoint, string username, string session)
    {
        var eventTopic = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "Login");

        var ev = _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Low, at, eventTopic);

        var endpointTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Endpoint, endpoint);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.ConnectedFrom, endpointTopic);

        var userTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.User, username);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.AuthenticatedAs, userTopic);

        var sessionTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Session, session);

        _db.Associations.Associate(_handle!, userTopic, Event.Associations.CreatedSession, sessionTopic);

        return ev;
    }

    public IEvent InsertFailedLoginEvent(DateTime at, string endpoint, string username)
    {
        var eventTopic = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "Login failed");

        var ev = _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Critical, at, eventTopic);

        var endpointTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Endpoint, endpoint);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.ConnectedFrom, endpointTopic);

        var userTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.User, username);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.LoginFailed, userTopic);

        return ev;
    }

    public IEvent InsertServiceEvent(DateTime at, bool started, string serviceName)
    {
        var eventTopic = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "Service status changed");

        var ev = _db.Events.NewEvent(_handle!, Event.EventKinds.Service, ESeverity.Medium, at, eventTopic);

        var serviceTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.Service, serviceName);

        _db.Associations.Associate(
            _handle!,
            eventTopic,
            started
                ? Event.Associations.Started
                : Event.Associations.Stopped,
            serviceTopic);

        return ev;
    }

    public (IEvent, ITopic) InsertUserCreatedEvent(DateTime at, string username)
    {
        var eventTopic = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "User created");

        var ev = _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Medium, at, eventTopic);

        var userTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.User, username);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.Created, userTopic);

        return (ev, userTopic);
    }

    public (IEvent, ITopic) InsertUserDeletedEvent(DateTime at, string username)
    {
        var eventTopic = _db!.Topics.NewTopic(_handle!, Event.TopicKinds.Event, "User deleted");

        var ev = _db.Events.NewEvent(_handle!, Event.EventKinds.Security, ESeverity.Critical, at, eventTopic);

        var userTopic = _db.Topics.NewTopic(_handle!, Event.TopicKinds.User, username);

        _db.Associations.Associate(_handle!, eventTopic, Event.Associations.Deleted, userTopic);

        return (ev, userTopic);
    }

    public static bool Equals(
        IEdge edge,
        ITopicKind leftKind,
        string leftDisplayName,
        IAssociation assoc,
        ITopicKind rightKind,
        string rightDisplayName)
        => leftKind.Id == edge.Left.Kind.Id
           && leftKind.Name == edge.Left.Kind.Name
           && leftDisplayName == edge.Left.DisplayName
           && assoc.Id == edge.Association.Id
           && assoc.Name == edge.Association.Name
           && rightKind.Id == edge.Right.Kind.Id
           && rightKind.Name == edge.Right.Kind.Name
           && rightDisplayName == edge.Right.DisplayName;
};