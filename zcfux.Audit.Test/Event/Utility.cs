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
using System.Net;
using zcfux.Audit.LinqToPg;
using zcfux.Translation.Data;

namespace zcfux.Audit.Test.Event;

sealed class Utility
{
    readonly IAuditDb _auditDb;
    readonly ITranslationDb _translationDb;
    readonly object _handle;

    ITextResource _loginEvent = default!;
    ITextResource _loginFailedEvent = default!;
    ITextResource _serviceStatusEvent = default!;
    ITextResource _userCreatedEvent = default!;
    ITextResource _userDeletedEvent = default!;

    public readonly TextCategory TextCategory = new(1, "Audit");
    
    public Utility(IAuditDb db, ITranslationDb translationDb, object handle)
        => (_auditDb, _translationDb, _handle) = (db, translationDb, handle);

    public void InsertDefaultEntries()
    {
        WriteDefaultAuditEntries();
        WriteDefaultTranslationEntries();
    }

    void WriteDefaultAuditEntries()
    {
        _auditDb.Events.InsertEventKind(_handle, EventKinds.Security);
        _auditDb.Events.InsertEventKind(_handle, EventKinds.Service);

        _auditDb.Topics.InsertTopicKind(_handle, TopicKinds.Event);
        _auditDb.Topics.InsertTopicKind(_handle, TopicKinds.Endpoint);
        _auditDb.Topics.InsertTopicKind(_handle, TopicKinds.User);
        _auditDb.Topics.InsertTopicKind(_handle, TopicKinds.Session);
        _auditDb.Topics.InsertTopicKind(_handle, TopicKinds.Service);

        _auditDb.Associations.InsertAssociation(_handle, Associations.ConnectedFrom);
        _auditDb.Associations.InsertAssociation(_handle, Associations.AuthenticatedAs);
        _auditDb.Associations.InsertAssociation(_handle, Associations.LoginFailed);
        _auditDb.Associations.InsertAssociation(_handle, Associations.CreatedSession);
        _auditDb.Associations.InsertAssociation(_handle, Associations.Started);
        _auditDb.Associations.InsertAssociation(_handle, Associations.Stopped);
        _auditDb.Associations.InsertAssociation(_handle, Associations.Created);
        _auditDb.Associations.InsertAssociation(_handle, Associations.Deleted);
    }

    void WriteDefaultTranslationEntries()
    {
        _translationDb.WriteCategory(_handle, TextCategory);

        var deDE = new Locale(1, "de-DE");

        _translationDb.WriteLocale(_handle, deDE);

        var enUS = new Locale(2, "en-US");

        _translationDb.WriteLocale(_handle, enUS);

        _loginEvent = _translationDb.NewTextResource(_handle, TextCategory, "login-succeeded");

        _translationDb.LocalizeText(_handle, _loginEvent, deDE, "Login");
        _translationDb.LocalizeText(_handle, _loginEvent, enUS, "Login");

        _loginFailedEvent = _translationDb.NewTextResource(_handle, TextCategory, "login-failed");

        _translationDb.LocalizeText(_handle, _loginFailedEvent, deDE, "Login fehlgeschlagen");
        _translationDb.LocalizeText(_handle, _loginFailedEvent, enUS, "Login failed");

        _userCreatedEvent = _translationDb.NewTextResource(_handle, TextCategory, "user-created");

        _translationDb.LocalizeText(_handle, _userCreatedEvent, deDE, "Benutzer erstellt");
        _translationDb.LocalizeText(_handle, _userCreatedEvent, enUS, "User created");

        _userDeletedEvent = _translationDb.NewTextResource(_handle, TextCategory, "user-deleted");

        _translationDb.LocalizeText(_handle, _userDeletedEvent, deDE, "Benutzer gelöscht");
        _translationDb.LocalizeText(_handle, _userDeletedEvent, enUS, "User deleted");

        _serviceStatusEvent = _translationDb.NewTextResource(_handle, TextCategory, "service-status-changed");
        
        _translationDb.LocalizeText(_handle, _serviceStatusEvent, deDE, "Servicestatus geändert");
        _translationDb.LocalizeText(_handle, _serviceStatusEvent, enUS, "Service status changed");
    }

    public IEvent InsertLoginEvent(DateTime at, string endpoint, string username, string session)
    {
        var eventTopic = _auditDb!.Topics.NewTranslatableTopic(_handle!, TopicKinds.Event, _loginEvent);

        var ev = _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Low, at, eventTopic);

        var endpointTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Endpoint,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, endpoint));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.ConnectedFrom, endpointTopic);

        var userTopic = _auditDb.Topics.NewTopic(_handle!,
            TopicKinds.User,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, username));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.AuthenticatedAs, userTopic);

        var sessionTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Session,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, session));

        _auditDb.Associations.Associate(_handle!, userTopic, Associations.CreatedSession, sessionTopic);

        return ev;
    }

    public IEvent InsertFailedLoginEvent(DateTime at, string endpoint, string username)
    {
        var eventTopic = _auditDb!.Topics.NewTranslatableTopic(_handle!, TopicKinds.Event, _loginFailedEvent);

        var ev = _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Critical, at, eventTopic);

        var endpointTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Endpoint,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, endpoint));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.ConnectedFrom, endpointTopic);

        var userTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.User,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, username));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.LoginFailed, userTopic);

        return ev;
    }

    public IEvent InsertServiceEvent(DateTime at, bool started, string serviceName)
    {
        var eventTopic = _auditDb!.Topics.NewTranslatableTopic(_handle!, TopicKinds.Event, _serviceStatusEvent);

        var ev = _auditDb.Events.NewEvent(_handle!, EventKinds.Service, ESeverity.Medium, at, eventTopic);

        var serviceTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.Service,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, serviceName));

        _auditDb.Associations.Associate(
            _handle!,
            eventTopic,
            started
                ? Associations.Started
                : Associations.Stopped,
            serviceTopic);

        return ev;
    }

    public (IEvent, ITopic) InsertUserCreatedEvent(DateTime at, string username)
    {
        var eventTopic = _auditDb!.Topics.NewTranslatableTopic(_handle!, TopicKinds.Event, _userCreatedEvent);

        var ev = _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Medium, at, eventTopic);

        var userTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.User,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, username));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.Created, userTopic);

        return (ev, userTopic);
    }

    public (IEvent, ITopic) InsertUserDeletedEvent(DateTime at, string username)
    {
        var eventTopic = _auditDb!.Topics.NewTranslatableTopic(_handle!, TopicKinds.Event, _userDeletedEvent);

        var ev = _auditDb.Events.NewEvent(_handle!, EventKinds.Security, ESeverity.Critical, at, eventTopic);

        var userTopic = _auditDb.Topics.NewTopic(
            _handle!,
            TopicKinds.User,
            _translationDb.GetOrCreateTextResource(_handle, TextCategory, username));

        _auditDb.Associations.Associate(_handle!, eventTopic, Associations.Deleted, userTopic);

        return (ev, userTopic);
    }

    public static bool Equals(
        ILocalizedEdge edge,
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