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
using System.Reflection;
using Castle.DynamicProxy;
using zcfux.Logging;

namespace zcfux.Telemetry.Device;

sealed class ApiInterceptor : IInterceptor, IProxy
{
    sealed record Command(string Topic, uint TimeToLive, uint ResponseTimeout);

    sealed record Event(IProducer Producer, Type ParameterType);

    readonly ConcurrentDictionary<int, Action<byte[]>> _pendingResponses = new();

    readonly Type _apiType;

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly NodeDetails _node;

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
        None = 0,
        Initialized = 1,
        Online = 2,
        Compatible = 4
    };

    readonly object _stateLock = new();
    EFlag _state = EFlag.None;

    readonly ManualResetEvent _apiInfoReceived = new(false);

    readonly object _detectedVersionLock = new();
    string? _detectedVersion;

    public ApiInterceptor(Type type, Options options)
    {
        _apiType = type;

        (_node, _connection, _serializer, _logger) = options;

        var attr = _apiType
            .GetCustomAttributes(typeof(ApiAttribute), false)
            .OfType<ApiAttribute>()
            .Single();

        _apiTopic = attr.Topic;
        _version = attr.Version;

        RegisterCommands();
        RegisterEvents();

        _connection.ConnectedAsync += ConnectedAsync;
        _connection.DisconnectedAsync += DisconnectedAsync;
        _connection.DeviceStatusReceivedAsync += DeviceStatusReceivedAsync;
        _connection.ApiInfoReceivedAsync += ApiInfoReceivedAsync;
        _connection.ApiMessageReceivedAsync += ApiMessageReceivedAsync;
        _connection.ResponseReceivedAsync += ResponseReceivedAsync;

        if (_connection.IsConnected)
        {
            InitializeConnectionAsync().Wait();
        }
    }

    void RegisterCommands()
    {
        _logger?.Debug("Discovering commands (client=`{0}', api=`{1}').", _connection.ClientId, _apiTopic);

        var m = new Dictionary<string, Command>();

        foreach (var itf in _apiType.GetAllInterfaces())
        {
            var methods = itf.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods)
            {
                if (method.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault() is CommandAttribute attr)
                {
                    if (method.ReturnType == typeof(void)
                        || method.ReturnType == typeof(Task)
                        || method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var command = new Command(attr.Topic, attr.TimeToLive, attr.ResponseTimeout);

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
        }

        _commands = m;
    }

    void RegisterEvents()
    {
        _logger?.Debug("Discovering events (client=`{0}', api=`{1}').", _connection.ClientId, _apiTopic);

        var events = new Dictionary<string, Event>();
        var eventGetters = new Dictionary<string, Event>();

        foreach (var itf in _apiType.GetAllInterfaces())
        {
            var props = itf.GetProperties(BindingFlags.Instance | BindingFlags.Public);

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

                        var ctor = typeof(Producer<>)
                            .MakeGenericType(parameterType)
                            .GetConstructor(new[] { typeof(bool) });

                        var producer = ctor!.Invoke(new object?[] { false }) as IProducer;

                        var ev = new Event(producer!, parameterType);

                        events.Add(attr.Topic, ev);
                        eventGetters.Add(prop.Name, ev);
                    }
                }
            }
        }

        _events = events;
        _eventGetters = eventGetters;
    }

    async Task ConnectedAsync(EventArgs e)
    {
        _logger?.Debug("Proxy (client=`{0}') connected.", _connection.ClientId);

        await InitializeConnectionAsync();
    }

    async Task InitializeConnectionAsync()
    {
        if ((State & EFlag.Initialized) == 0)
        {
            try
            {
                var tasks = new[]
                {
                    _connection
                        .SubscribeToDeviceStatusAsync(new NodeFilter(_node)),

                    _connection
                        .SubscribeToApiInfoAsync(new ApiFilter(_node, _apiTopic)),

                    _connection
                        .SubscribeToApiMessagesAsync(_node, _apiTopic, EDirection.Out),

                    _connection
                        .SubscribeResponseAsync(_node)
                };

                await Task.WhenAll(tasks.ToArray());

                foreach (var ev in _events.Values)
                {
                    ev.Producer.Enable();
                }

                WriteState(state => (state | EFlag.Initialized));
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }
        else
        {
            try
            {
                foreach (var ev in _events.Values)
                {
                    ev.Producer.Enable();
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }
    }

    Task DisconnectedAsync(EventArgs e)
    {
        _logger?.Debug("Proxy (client=`{0}') disconnected.", _connection.ClientId);

        try
        {
            foreach (var ev in _events.Values)
            {
                ev.Producer.Disable();
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        WriteState(state => (state & ~EFlag.Online));

        return Task.CompletedTask;
    }

    Task DeviceStatusReceivedAsync(NodeStatusEventArgs e)
    {
        if (e.Node.Equals(_node))
        {
            if (e.Status == ENodeStatus.Connecting || e.Status == ENodeStatus.Offline || e.Status == ENodeStatus.Error)
            {
                WriteState(state => (state & ~EFlag.Online));
            }
            else
            {
                WriteState(state => (state | EFlag.Online));
            }
        }

        return Task.CompletedTask;
    }

    async Task ApiInfoReceivedAsync(ApiInfoEventArgs e)
    {
        if (e.Node.Equals(_node)
            && e.Api.Equals(_apiTopic)
            && (!e.Version.Equals(DetectedVersion) || (State & EFlag.Compatible) == EFlag.None))
        {
            if (VersionIsCompatible(e.Version))
            {
                _logger?.Debug("Proxy (client=`{0}') found compatible endpoint.", _connection.ClientId);

                WriteState(state => (state | EFlag.Compatible));

                await _connection
                    .SubscribeToApiMessagesAsync(
                        _node,
                        _apiTopic,
                        EDirection.Out);
            }
            else
            {
                WriteState(state => (state & ~EFlag.Compatible));
            }

            DetectedVersion = e.Version;

            _apiInfoReceived.Set();
        }
    }

    bool VersionIsCompatible(string version)
    {
        var (major1, minor1) = Version.Parse(_version);
        var (major2, minor2) = Version.Parse(version);

        return (major1 == major2)
               && (minor1 <= minor2);
    }

    Task ApiMessageReceivedAsync(ApiMessageEventArgs e)
    {
        if (e.Node.Equals(_node)
            && e.Api.Equals(_apiTopic)
            && e.Direction == EDirection.Out)
        {
            if ((State & EFlag.Compatible) == EFlag.Compatible
                && _events.TryGetValue(e.Topic, out var ev))
            {
                if ((State & EFlag.Online) == EFlag.None)
                {
                    _logger?.Warn(
                        "Proxy (client=`{0}') publishes message from offline device (domain=`{1}', kind=`{2}', id={3}).",
                        _connection.ClientId,
                        _node.Domain,
                        _node.Kind,
                        _node.Id);
                }

                try
                {
                    var payload = _serializer.Deserialize(e.Payload, ev.ParameterType);

                    _logger?.Trace(
                        "Proxy (client=`{0}') produces value (domain=`{1}', kind=`{2}', id={3}, api=`{4}', topic=`{5}'): `{6}'",
                        _connection.ClientId,
                        _node.Domain,
                        _node.Kind,
                        _node.Id,
                        e.Api,
                        e.Topic,
                        payload!);

                    ev.Producer.Write(payload!);
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }
        }

        return Task.CompletedTask;
    }

    Task ResponseReceivedAsync(ResponseEventArgs e)
    {
        if (e.Node.Equals(_node))
        {
            _logger?.Debug("Proxy (client=`{0}') received response (message={1}).", _connection.ClientId, e.MessageId);

            if (_pendingResponses.TryRemove(e.MessageId, out var fn))
            {
                try
                {
                    fn(e.Payload);
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

        return Task.CompletedTask;
    }

    public void Intercept(IInvocation invocation)
    {
        if (invocation.Method.Name.Equals("ReleaseProxy"))
        {
            ReleaseProxy();
        }
        else if (invocation.Method.Name.StartsWith("get_"))
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
        var propertyName = invocation.Method.Name[4..];

        if (!_eventGetters.TryGetValue(propertyName, out var ev))
        {
            throw new ApplicationException($"Property `{propertyName}' not found.");
        }

        _apiInfoReceived.WaitOne(TimeSpan.FromSeconds(1));

        if ((State & EFlag.Compatible) == EFlag.Compatible)
        {
            _logger?.Trace(
                "API `{0}' is compatible, `{1}' returning producer (client=`{2}', domain=`{3}', kind=`{4}', id={5}).",
                _apiTopic,
                propertyName,
                _connection.ClientId,
                _node.Domain,
                _node.Kind,
                _node.Id);

            invocation.ReturnValue = ev.Producer;
        }
        else
        {
            _logger?.Warn(
                "API `{0}' not compatible, `{1}' returning empty enumerable (client=`{2}', domain=`{3}', kind=`{4}', id={5}).",
                _apiTopic,
                propertyName,
                _connection.ClientId,
                _node.Domain,
                _node.Kind,
                _node.Id);

            var producedType = ev
                .Producer
                .GetType()
                .GetGenericArguments()
                .Single();

            invocation.ReturnValue = Activator
                .CreateInstance(
                    typeof(EmptyAsyncEnumerable<>)
                        .MakeGenericType(producedType));
        }
    }

    void InterceptCommand(IInvocation invocation)
    {
        object? returnValue;

        var signature = MethodSignature(invocation.Method);

        if (_commands.TryGetValue(signature, out var command))
        {
            var parameter = invocation.Arguments.SingleOrDefault();

            if (invocation.Method.ReturnType == typeof(void)
                || invocation.Method.ReturnType == typeof(Task))
            {
                returnValue = SendCommandAsync(
                    command.Topic,
                    command.TimeToLive,
                    parameter);
            }
            else
            {
                var genericType = invocation
                    .Method
                    .ReturnType
                    .GetGenericArguments()
                    .Single();

                var requestResponseAsync = GetType()
                    .GetMethod("RequestResponseAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(genericType);

                returnValue = requestResponseAsync.Invoke(
                    this,
                    new[] { command.Topic, command.TimeToLive, command.ResponseTimeout, parameter });
            }
        }
        else
        {
            throw new ApplicationException(
                $"Method with signature `{signature}' not found.");
        }

        if (invocation.Method.ReturnType == typeof(void))
        {
            (returnValue as IAsyncResult)!.AsyncWaitHandle.WaitOne();
        }
        else
        {
            invocation.ReturnValue = returnValue;
        }
    }

    async Task SendCommandAsync(string topic, uint timeToLive, object? parameter)
    {
        if (!await WaitForCompatibilityAsync(TimeSpan.FromSeconds(timeToLive)))
        {
            throw new OperationCanceledException();
        }

        _logger?.Debug(
            "Sending command (domain=`{0}', kind=`{1}', id={2}, api=`{3}', topic=`{4}').",
            _node.Domain,
            _node.Kind,
            _node.Id,
            _apiTopic,
            topic);

        var message = new ApiMessage(
            _node,
            _apiTopic,
            topic,
            new MessageOptions(Retain: false, TimeToLive: timeToLive),
            EDirection.In,
            parameter);

        await _connection.SendApiMessageAsync(message);
    }

    async Task<T> RequestResponseAsync<T>(string topic, uint timeToLive, uint responseTimeout, object? parameter)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!await WaitForCompatibilityAsync(TimeSpan.FromSeconds(timeToLive)))
        {
            throw new OperationCanceledException();
        }

        var secondsLeft = (uint)Math.Floor((timeToLive - stopwatch.Elapsed.TotalSeconds));

        var messageId = NextMessageId();

        var waitForResponseAsync = WaitForResponseAsync<T>(messageId, responseTimeout);

        _logger?.Debug(
            "Sending request (domain=`{0}', kind=`{1}', id={2}, api=`{3}', topic=`{4}', message id={5}).",
            _node.Domain,
            _node.Kind,
            _node.Id,
            _apiTopic,
            topic,
            messageId);

        var message = new ApiMessage(
            _node,
            _apiTopic,
            topic,
            new MessageOptions(Retain: false, TimeToLive: secondsLeft),
            EDirection.In,
            parameter,
            $"r/{_connection.ClientId}/{_node.Domain}/{_node.Kind}/{_node.Id}",
            messageId);

        await _connection.SendApiMessageAsync(message);

        return await waitForResponseAsync;
    }

    async Task<T> WaitForResponseAsync<T>(int messageId, uint responseTimeout)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();

        _pendingResponses[messageId] = payload =>
        {
            try
            {
                var parameter = _serializer.Deserialize<T>(payload);

                taskCompletionSource.SetResult(parameter!);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        };

        var responseTask = taskCompletionSource.Task;

        if (await Task.WhenAny(
                responseTask,
                Task.Delay(TimeSpan.FromSeconds(responseTimeout)))
            != taskCompletionSource.Task)
        {
            throw new OperationCanceledException();
        }

        return responseTask.Result;
    }

    Task<bool> WaitForCompatibilityAsync(TimeSpan timeout)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        if ((State & EFlag.Compatible) == EFlag.Compatible)
        {
            taskCompletionSource.SetResult(true);
        }
        else
        {
            var registration = ThreadPool.RegisterWaitForSingleObject(
                _apiInfoReceived,
                (_, timedOut) =>
                {
                    if (timedOut)
                    {
                        taskCompletionSource.TrySetCanceled();
                    }
                    else
                    {
                        var compatible = (State & EFlag.Compatible) == EFlag.Compatible;

                        taskCompletionSource.TrySetResult(compatible);
                    }
                },
                null,
                timeout,
                true);

            taskCompletionSource.Task.ContinueWith(_ =>
            {
                registration.Unregister(null);
            }, CancellationToken.None);
        }

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

    void WriteState(Func<EFlag, EFlag> fn)
    {
        EFlag newState;

        lock (_stateLock)
        {
            newState = fn(_state);

            _state = newState;
        }

        _logger?.Debug("Proxy (client=`{0}' state changed: {1}", _connection.ClientId, newState);
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

    public void ReleaseProxy()
    {
        _connection.ConnectedAsync -= ConnectedAsync;
        _connection.DisconnectedAsync -= DisconnectedAsync;
        _connection.DeviceStatusReceivedAsync -= DeviceStatusReceivedAsync;
        _connection.ApiInfoReceivedAsync -= ApiInfoReceivedAsync;
        _connection.ApiMessageReceivedAsync -= ApiMessageReceivedAsync;
        _connection.ResponseReceivedAsync -= ResponseReceivedAsync;
    }
}