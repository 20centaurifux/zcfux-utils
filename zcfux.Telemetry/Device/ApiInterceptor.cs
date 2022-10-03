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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Castle.DynamicProxy;
using System.Reflection;
using Humanizer;
using zcfux.Logging;

namespace zcfux.Telemetry.Device;

internal sealed class ApiInterceptor : IInterceptor, IDisposable
{
    static readonly TimeSpan ResponseTimeout = TimeSpan.FromMinutes(1);

    static readonly CultureInfo LoggingCulture = CultureInfo.GetCultureInfo("en-US");

    sealed record Command(string Topic, uint TimeToLive);

    sealed record Event(IProducer Producer, Type ParameterType);

    sealed record PendingResponse(Action<byte[]> SetResult, Action Cancel, Stopwatch Stopwatch);

    readonly ConcurrentDictionary<int, PendingResponse> _pendingResponses = new();

    readonly Type _apiType;

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly DeviceDetails _device;

    readonly string _apiTopic;
    readonly string _version;

    readonly object _messageIdLock = new();
    int _messageId;

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

    readonly Task _cleanPendingResponsesTask;

    readonly CancellationTokenSource _cancellationTokenSource = new();

    public ApiInterceptor(Type type, Options options)
    {
        _apiType = type;

        (_device, _connection, _serializer, _logger) = options;

        var attr = _apiType
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
        _connection.ResponseReceived += ResponseReceived;

        _cleanPendingResponsesTask = CleanPendingResponsesAsync();
    }

    void RegisterCommands()
    {
        _logger?.Debug("Discovering commands (client=`{0}', api=`{1}').", _connection.ClientId, _apiTopic);

        var m = new Dictionary<string, Command>();

        var methods = _apiType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        foreach (var method in methods)
        {
            if (method.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault() is CommandAttribute attr)
            {
                if (method.ReturnType == typeof(void)
                    || method.ReturnType == typeof(Task)
                    || method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var command = new Command(attr.Topic, attr.TimeToLive);

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

        var props = _apiType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

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
                        "Found event (client=`{0}', api=`{1}', topic=`{2}', type={3}).",
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

        ExchangeState(EFlag.Connecting);

        var tasks = new[]
        {
            _connection
                .SubscribeToDeviceStatusAsync(new DeviceFilter(_device), CancellationToken.None),

            _connection
                .SubscribeToApiInfoAsync(new ApiFilter(_device, _apiTopic), CancellationToken.None),

            _connection
                .SubscribeToApiMessagesAsync(_device, _apiTopic, EDirection.Out, CancellationToken.None),

            _connection
                .SubscribeResponseAsync(_device, CancellationToken.None)
        };

        Task.WaitAll(tasks);
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
        var (major1, minor1) = Version.Parse(_version);
        var (major2, minor2) = Version.Parse(version);

        return (major1 == major2)
               && (minor1 <= minor2);
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

    void ResponseReceived(object? sender, ResponseEventArgs e)
    {
        _logger?.Debug("Response (message id={0}) received.", e.MessageId);

        if (_pendingResponses.TryRemove(e.MessageId, out var pendingResponse))
        {
            try
            {
                pendingResponse.SetResult(e.Payload);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }
        else
        {
            _logger?.Warn("Pending response (message id={0}) not found.", e.MessageId);
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

        var parameter = invocation
            .Arguments
            .SingleOrDefault();

        if (invocation.Method.ReturnType == typeof(void)
            || invocation.Method.ReturnType == typeof(Task))
        {
            var task = SendCommand(command.Topic, command.TimeToLive, parameter);

            if (invocation.Method.ReturnType == typeof(void))
            {
                task.Wait();
            }
            else
            {
                invocation.ReturnValue = task;
            }
        }
        else
        {
            var task = SendRequest(
                command.Topic,
                command.TimeToLive,
                parameter,
                invocation.Method.ReturnType.GetGenericArguments().Single());

            invocation.ReturnValue = task;
        }
    }

    Task SendCommand(string topic, uint timeToLive, object? parameter)
    {
        _logger?.Debug(
            "Sending command (domain=`{0}', kind=`{1}', id={2}, api=`{3}', topic=`{4}').",
            _device.Domain,
            _device.Kind,
            _device.Id,
            _apiTopic,
            topic);

        var message = new ApiMessage(
            _device,
            _apiTopic,
            topic,
            new MessageOptions(Retain: false, TimeToLive: timeToLive),
            EDirection.In,
            parameter);

        var task = _connection.SendApiMessageAsync(message, CancellationToken.None);

        return task;
    }

    Task SendRequest(string topic, uint timeToLive, object? parameter, Type returnType)
    {
        var messageId = NextMessageId();

        _logger?.Debug(
            "Sending request (domain=`{0}', kind=`{1}', id={2}, api=`{3}', topic=`{4}', message id={5}).",
            _device.Domain,
            _device.Kind,
            _device.Id,
            _apiTopic,
            topic,
            messageId);

        var t = typeof(TaskCompletionSource<>).MakeGenericType(returnType);

        dynamic taskCompletionSource = Activator.CreateInstance(t)!;

        var setResult = t.GetMethod("SetResult");

        var setCanceled = t.GetMethod(
            "SetCanceled",
            BindingFlags.Instance | BindingFlags.Public,
            Array.Empty<Type>());

        _pendingResponses[messageId] = new PendingResponse(
            payload =>
            {
                var p = _serializer.Deserialize(payload, returnType);

                setResult?.Invoke(taskCompletionSource, new[] { p });
            },
            () =>
            {
                setCanceled?.Invoke(taskCompletionSource, Array.Empty<object>());
            },
            Stopwatch.StartNew());

        var message = new ApiMessage(
            _device,
            _apiTopic,
            topic,
            new MessageOptions(Retain: false, TimeToLive: timeToLive),
            EDirection.In,
            parameter,
            $"{_device.Domain}/{_device.Kind}/{_device.Id}/r",
            messageId);

        _connection.SendApiMessageAsync(message, CancellationToken.None).Wait();

        return taskCompletionSource.Task;
    }

    static string MethodSignature(MethodInfo method)
    {
        var parameterType = method
            .GetParameters()
            .SingleOrDefault()
            ?.Name;

        return $"{method.ReturnType} {method.Name}({parameterType ?? string.Empty})";
    }

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

    int NextMessageId()
    {
        lock (_messageIdLock)
        {
            if (_messageId == int.MaxValue)
            {
                _messageId = 0;
            }

            return ++_messageId;
        }
    }

    Task CleanPendingResponsesAsync()
    {
        return Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var expiredMessageIds = new List<int>();

                var timeout = ResponseTimeout;

                foreach (var (messageId, pendingResponse) in _pendingResponses)
                {
                    var elapsed = pendingResponse.Stopwatch.Elapsed;

                    if (elapsed >= ResponseTimeout)
                    {
                        expiredMessageIds.Add(messageId);
                    }
                    else
                    {
                        var diff = (ResponseTimeout - elapsed);

                        if (diff < timeout)
                        {
                            timeout = diff;
                        }
                    }
                }

                _logger?.Debug(
                    "Cleaning pending responses in {0}.",
                    timeout.Humanize(culture: LoggingCulture));

                await Task.WhenAll(
                    CancelResponsesAsync(expiredMessageIds.ToArray()),
                    Task.Delay(timeout));
            }
        }, _cancellationTokenSource.Token);
    }

    Task CancelResponsesAsync(int[] messageIds)
    {
        return Task.Run(() =>
        {
            foreach (var messageId in messageIds)
            {
                if (_pendingResponses.TryRemove(messageId, out var pendingResponse))
                {
                    try
                    {
                        _logger?.Debug("Cancelling request (message id={0}).", messageId);

                        pendingResponse.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex);
                    }
                }
            }
        }, _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();

        _cleanPendingResponsesTask.Wait();
    }
}