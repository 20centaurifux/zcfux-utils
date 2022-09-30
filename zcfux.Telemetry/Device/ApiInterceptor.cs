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
using Castle.DynamicProxy;
using System.Reflection;
using System.Text.RegularExpressions;
using zcfux.Logging;

namespace zcfux.Telemetry.Device;

internal sealed class ApiInterceptor<TApi> : IInterceptor
{
    sealed record Command(string Topic, uint TimeToLive, Type ParameterType, bool Blocking);
    sealed record Event(IProducer Producer, Type ParameterType);

    static readonly Regex VersionRegex = new("^([\\d+])\\.([\\d]+)$");

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly DeviceDetails _device;

    readonly string _apiTopic;
    readonly string _version;

    IReadOnlyDictionary<string, Command> _commands = default!;
    IReadOnlyDictionary<string, Event> _events = default!;
    IReadOnlyDictionary<string, Event> _eventGetters = default!;

    [Flags]
    enum EFlag
    {
        Offline = 1,
        Connecting = 2,
        Online = 4,
        Compatible = 8,
        Incompatible = 16
    };

    readonly object _stateLock = new();
    EFlag _state = EFlag.Offline;

    readonly object _detectedVersionLock = new();
    string? _detectedVersion;

    public ApiInterceptor(Options options)
    {
        (_device, _connection, _serializer, _logger) = options;

        var attr = typeof(TApi)
            .GetCustomAttributes(typeof(ApiAttribute), false)
            .OfType<ApiAttribute>()
            .Single();

        _apiTopic = attr.Topic;
        _version = attr.Version;

        RegisterCommands();
        RegisterEvents();

        _connection.Connected += Connected;
        _connection.Disconnected += Disconnected;
        _connection.DeviceStatusReceived += DeviceStatusReceived;
        _connection.ApiInfoReceived += ApiInfoReceived;
        _connection.ApiMessageReceived += ApiMessageReceived;
    }

    void RegisterCommands()
    {
        _logger?.Debug("Discovering commands (client=`{0}', api=`{1}').", _connection.ClientId,  _apiTopic);

        var m = new Dictionary<string, Command>();

        var methods = typeof(TApi).GetMethods(BindingFlags.Instance | BindingFlags.Public);

        foreach (var method in methods)
        {
            if (method.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault() is CommandAttribute attr)
            {
                if (method.ReturnType == typeof(Task) || method.ReturnType == typeof(void))
                {
                    var parameterType = method
                        .GetParameters()
                        .Single()
                        .ParameterType;

                    var command = new Command(
                        attr.Topic,
                        attr.TimeToLive,
                        parameterType,
                        method.ReturnType == typeof(void));

                    var signature = MethodSignature(method);

                    _logger?.Debug(
                        "Found command (client=`{0}', api=`{1}', topic=`{2}', signature=`{3}').",
                        _connection.ClientId,
                        _apiTopic,
                        attr.Topic,
                        signature);

                    m.Add(signature, command);
                }
                else
                {
                    _logger?.Debug(
                        "Command (client=`{0}', api=`{1}', topic=`{2}') has unsupported return type ({3}).",
                        _connection.ClientId,
                        _apiTopic,
                        attr.Topic,
                        method.ReturnType);
                }
            }
        }

        _commands = m;
    }

    void RegisterEvents()
    {
        _logger?.Debug("Discovering events (client=`{0}', api=`{1}').", _connection.ClientId, _apiTopic);

        var events = new Dictionary<string, Event>();
        var eventGetters = new Dictionary<string, Event>();

        var props = typeof(TApi).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in props)
        {
            if (prop.GetCustomAttributes(typeof(EventAttribute), false).SingleOrDefault() is EventAttribute attr)
            {
                if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    var parameterType = prop
                        .PropertyType
                        .GetGenericArguments()
                        .Single();

                    _logger?.Debug(
                        "Found event (client=`{0}', api=`{1}', topic=`{2}', type={4}).",
                        _connection.ClientId,
                        _apiTopic,
                        attr.Topic,
                        parameterType);

                    var producer = (Activator.CreateInstance(
                        typeof(Producer<>).MakeGenericType(parameterType)) as IProducer)!;

                    var ev = new Event(producer, parameterType);

                    events.Add(attr.Topic, ev);
                    eventGetters.Add(prop.Name, ev);
                }
            }
        }

        _events = events;
        _eventGetters = eventGetters;
    }

    void Connected(object? sender, EventArgs e)
    {
        var (domain, kind, id) = _device;

        _logger?.Debug(
            "Proxy `{0}/{1}/{2}/{3}' connected.",
            domain,
            kind,
            id,
            _apiTopic);

        var tasks = new[]
        {
            _connection
                .SubscribeToDeviceStatusAsync(_device, CancellationToken.None),

            _connection
                .SubscribeToApiInfoAsync(_device, _apiTopic, CancellationToken.None),

            _connection
                .SubscribeToApiMessagesAsync(_device, _apiTopic, EDirection.Out, CancellationToken.None)
        };

        Task.WaitAll(tasks);

        SetStateFlags(EFlag.Connecting);
    }

    void Disconnected(object? sender, EventArgs e)
    {
        var (domain, kind, id) = _device;

        _logger?.Debug(
            "Proxy `{0}/{1}/{2}/{3}' disconnected.",
            domain,
            kind,
            id,
            _apiTopic);

        ExchangeState(EFlag.Offline);
    }

    void DeviceStatusReceived(object? sender, DeviceStatusEventArgs e)
    {
        if (e.Device.Equals(_device))
        {
            switch (e.Status)
            {
                case EDeviceStatus.Connecting:
                    ExchangeState(EFlag.Connecting);
                    break;

                case EDeviceStatus.Offline:
                    ExchangeState(EFlag.Offline);
                    break;

                case EDeviceStatus.Online:
                    SwapStateFlags(EFlag.Connecting, EFlag.Online);
                    break;
            }
        }
    }

    void ApiInfoReceived(object? sender, ApiInfoEventArgs e)
    {
        if (e.Device.Equals(_device)
            && e.Api.Equals(_apiTopic)
            && !e.Version.Equals(DetectedVersion))
        {
            var (domain, kind, id) = _device;

            if (VersionIsCompatible(e.Version))
            {
                _logger?.Debug(
                    "Proxy `{0}/{1}/{2}/{3}' found compatible endpoint (`{4}' >= `{5}').",
                    domain,
                    kind,
                    id,
                    _apiTopic,
                    _version,
                    e.Version);

                SetStateFlags(EFlag.Compatible);

                _connection
                    .SubscribeToApiMessagesAsync(
                        _device,
                        _apiTopic,
                        EDirection.Out,
                        CancellationToken.None)
                    .Wait();
            }
            else
            {
                SetStateFlags(EFlag.Incompatible);
            }

            DetectedVersion = e.Version;
        }
    }

    bool VersionIsCompatible(string version)
    {
        var compatible = false;

        var m2 = VersionRegex.Match(version);

        if (m2.Success)
        {
            var m1 = VersionRegex.Match(_version);

            if (m2.Success)
            {
                var desiredVersion = m1
                    .Groups
                    .Values
                    .Skip(1)
                    .Select(g => int.Parse(g.Value))
                    .ToArray();

                var discoveredVersion = m2
                    .Groups
                    .Values
                    .Skip(1)
                    .Select(g => int.Parse(g.Value))
                    .ToArray();

                compatible = (desiredVersion[0] == discoveredVersion[0])
                    && (desiredVersion[1] <= discoveredVersion[1]);
            }
        }

        return compatible;
    }

    void ApiMessageReceived(object? sender, ApiMessageEventArgs e)
    {
        if (e.Device.Equals(_device)
            && e.Api.Equals(_apiTopic)
            && e.Direction == EDirection.Out)
        {
            if ((State & EFlag.Incompatible) == 0
                && _events.TryGetValue(e.Topic, out var ev))
            {
                try
                {
                    var payload = _serializer.Deserialize(e.Payload, ev.ParameterType);

                    ev.Producer.Publish(payload!);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }
        }
    }

    public void Intercept(IInvocation invocation)
    {
        if (invocation.Method.Name.StartsWith("get_"))
        {
            InterceptGetter(invocation);
        }
        else
        {
            InterceptCommand(invocation);
        }
    }

    void InterceptGetter(IInvocation invocation)
    {
        var propertyName = invocation.Method.Name.Substring(4);

        if (!_eventGetters.TryGetValue(propertyName, out var ev))
        {
            throw new ApplicationException($"Property `{propertyName}' not found.");
        }

        invocation.ReturnValue = ev.Producer;
    }

    void InterceptCommand(IInvocation invocation)
    {
        var signature = MethodSignature(invocation.Method);

        if (!_commands.TryGetValue(signature, out var command))
        {
            throw new ApplicationException($"Method with signature `{signature}' not found.");
        }

        if ((State & EFlag.Incompatible) == EFlag.Incompatible)
        {
            throw new ApplicationException("Incompatible endpoint.");
        }

        var message = new ApiMessage(
            _device,
            _apiTopic,
            command.Topic,
            new MessageOptions(Retain: false, TimeToLive: command.TimeToLive),
            invocation.Arguments.Single());

        var task = _connection.SendApiMessageAsync(message, CancellationToken.None);

        if (command.Blocking)
        {
            task.Wait();
        }
        else
        {
            invocation.ReturnValue = task;
        }
    }

    static string MethodSignature(MethodInfo method)
        => $"{method.ReturnType} {method.Name}({method.GetParameters().Single().ParameterType})";

    void ExchangeState(EFlag newState)
    {
        lock (_stateLock)
        {
            ExchangeState_Unlocked(newState);
        }
    }

    void SetStateFlags(EFlag bitmask)
    {
        lock (_stateLock)
        {
            ExchangeState_Unlocked(_state | bitmask);
        }
    }

    void SwapStateFlags(EFlag unset, EFlag set)
    {
        lock (_stateLock)
        {
            var newState = (_state & ~unset);

            ExchangeState_Unlocked(newState | set);
        }
    }

    void ExchangeState_Unlocked(EFlag newState)
    {
        lock (_stateLock)
        {
            if (newState != _state)
            {
                _state = newState;

                _logger?.Debug(
                    "Proxy state (`{0}/{1}/{2}/{3}') changed: {4}",
                    _device.Domain,
                    _device.Kind,
                    _device.Id,
                    _apiTopic,
                    _state);
            }
        }
    }

    EFlag State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    string? DetectedVersion
    {
        get
        {
            lock (_detectedVersionLock)
            {
                return _detectedVersion;
            }
        }

        set
        {
            lock (_detectedVersionLock)
            {
                _detectedVersion = value;
            }
        }
    }
}