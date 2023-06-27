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
using System.Reflection;
using Castle.DynamicProxy.Internal;
using zcfux.Logging;

namespace zcfux.Telemetry.Node;

public class Client : IAsyncDisposable
{
    protected string Domain { get; }

    protected string Kind { get; }

    protected int Id { get; }

    protected ILogger? Logger { get; }

    readonly CancellationTokenSource _cancellationTokenSource = new();

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly TimeSpan _eventSubscriberTimeout;

    readonly IReadOnlyCollection<Api> _apis;
    readonly IReadOnlyCollection<Task> _eventSubscriptions;
    readonly IReadOnlyDictionary<MethodKey, Method> _methods;

    readonly PendingCommands _pendingCommands = new();
    Task? _pendingCommandsProcessor;

    readonly object _statusLock = new();
    EStatus _status = EStatus.Ok;

    const long No = 0;
    const long Yes = 1;

    long _initialized = No;
    long _shutdown = No;

    protected Client(Options options)
    {
        ((Domain, Kind, Id), _connection, _serializer, Logger, _eventSubscriberTimeout) = options;

        (_apis, _eventSubscriptions, _methods) = RegisterApis();
    }

    public virtual async Task SetupAsync()
    {
        if (Interlocked.CompareExchange(ref _initialized, Yes, No) == Yes)
        {
            throw new InvalidOperationException();
        }

        Logger?.Debug("Setup started");

        _connection.ConnectedAsync += ConnectedAsync;
        _connection.DisconnectedAsync += DisconnectedAsync;
        _connection.ApiMessageReceivedAsync += ApiMessageReceivedAsync;

        _pendingCommandsProcessor = ProcessPendingTasksAsync();

        if (_connection.IsConnected)
        {
            await ConnectedAsync(EventArgs.Empty);
        }

        Logger?.Debug("Setup completed");

    }

    protected EStatus ChangeStatus(EStatus status)
    {
        lock (_statusLock)
        {
            var previousStatus = _status;

            _status = status;

            return previousStatus;
        }
    }

    protected Task SendStatusAsync(CancellationToken cancellationToken = default)
        => _connection.SendStatusAsync(
            new NodeStatusMessage(
                new NodeDetails(Domain, Kind, Id), MapCurrentStatus()),
            cancellationToken);

    (IReadOnlyCollection<Api>, IReadOnlyCollection<Task>, IReadOnlyDictionary<MethodKey, Method>) RegisterApis()
    {
        var apis = new List<Api>();
        var subscriptions = new List<Task>();
        var methods = new Dictionary<MethodKey, Method>();

        Logger?.Debug("Discovering client (domain=`{0}', kind=`{1}', id={2}).", Domain, Kind, Id);

        foreach (var prop in GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (prop
                    .PropertyType
                    .GetCustomAttributes(typeof(ApiAttribute), false)
                    .SingleOrDefault() is ApiAttribute attr)
            {
                Logger?.Debug(
                    "Found api (type={0}, topic=`{1}', version=`{2}').",
                    prop.PropertyType.Name,
                    attr.Topic,
                    attr.Version);

                var api = new Api(
                    prop.PropertyType,
                    prop.GetValue(this)!,
                    attr.Topic,
                    attr.Version,
                    prop.PropertyType.GetAllInterfaces());

                apis.Add(api);

                subscriptions.AddRange(SubscribeToAsyncEnumerables(api));

                foreach (var (key, method) in RegisterMethods(api))
                {
                    methods.Add(key, method);
                }
            }
        }

        Logger?.Debug("Node api discovery completed.");

        return (apis, subscriptions, methods);
    }

    IEnumerable<Task> SubscribeToAsyncEnumerables(Api api)
    {
        Logger?.Debug("Searching for asynchronous enumerables (api=`{0}').", api.Topic);

        var props = api.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in props)
        {
            if (prop.GetCustomAttributes(typeof(EventAttribute), false).SingleOrDefault() is EventAttribute attr)
            {
                if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    var source = prop.GetValue(api.Instance)!;

                    var parameterType = source
                        .GetType()
                        .GetInterfaces()
                        .Single(intf => intf.FullName!.StartsWith("System.Collections.Generic.IAsyncEnumerable"))
                        .GetGenericArguments()
                        .Single();

                    Logger?.Debug(
                        "Found asynchronous enumerable (api=`{0}', topic=`{1}', parameter type={2}, retain={3}, ttl={4}s).",
                        api.Topic,
                        attr.Topic,
                        parameterType.Name,
                        attr.Retain,
                        attr.TimeToLive);

                    var method = typeof(Client)
                        .GetMethod(
                            "SubscribeToAsyncEnumerable",
                            BindingFlags.Instance | BindingFlags.NonPublic)!
                        .MakeGenericMethod(parameterType);

                    var result = method.Invoke(this, new[]
                    {
                        api.Topic,
                        attr.Topic,
                        new MessageOptions(attr.Retain, attr.TimeToLive),
                        source
                    }) as Task;

                    yield return result!;
                }
            }
        }
    }
    
    Task SubscribeToAsyncEnumerable<T>(string apiTopic, string topic, MessageOptions options, IAsyncEnumerable<T> source)
    {
        Logger?.Debug(
            "Starting event consumer task (api=`{0}', topic=`{1}').",
            apiTopic,
            topic);

        var enumeratorCreated = new AutoResetEvent(false);

        var task = Task.Run(async () =>
        {
            try
            {
                await using (var enumerator = source.GetAsyncEnumerator(_cancellationTokenSource.Token))
                {
                    enumeratorCreated.Set();

                    var moveNextTask = enumerator.MoveNextAsync().AsTask();
                    var timeoutTask = Task.Delay(_eventSubscriberTimeout);

                    var completed = false;

                    while (!completed)
                    {
                        var winner = await Task.WhenAny(moveNextTask, timeoutTask);

                        if (winner.IsCanceled)
                        {
                            throw new TaskCanceledException();
                        }

                        if (winner == moveNextTask)
                        {
                            if (moveNextTask.Result)
                            {
                                var current = enumerator.Current;

                                Logger?.Trace(
                                    "Received event value (api=`{0}', topic=`{1}', type={2}).",
                                    apiTopic,
                                    topic,
                                    typeof(T).Name);

                                if (current != null)
                                {
                                    var message = new ApiMessage(
                                        new NodeDetails(Domain, Kind, Id),
                                        apiTopic,
                                        topic,
                                        options,
                                        EDirection.Out,
                                        current);

                                    await _connection.SendApiMessageAsync(message, _cancellationTokenSource.Token);
                                }

                                moveNextTask = enumerator.MoveNextAsync().AsTask();
                                timeoutTask = Task.Delay(_eventSubscriberTimeout, _cancellationTokenSource.Token);
                            }
                            else
                            {
                                Logger?.Trace(
                                    "Event source (api=`{0}', topic=`{1}', type={2}) closed, completing task.",
                                    apiTopic,
                                    topic,
                                    typeof(T).Name);

                                completed = true;
                            }
                        }
                        else if (Interlocked.Read(ref _shutdown) == Yes)
                        {
                            Logger?.Trace(
                                "Event consumer timeout (api=`{0}', topic=`{1}', type={2}), completing task.",
                                apiTopic,
                                topic,
                                typeof(T).Name);

                            completed = true;
                        }
                        else
                        {
                            timeoutTask = Task.Delay(_eventSubscriberTimeout, _cancellationTokenSource.Token);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // {-_-}
            }
            catch (Exception ex)
            {
                Logger?.Error(ex);
            }
        });

        Logger?.Debug(
            "Waiting for event consumer task (api=`{0}', topic=`{1}').",
            apiTopic,
            topic);

        enumeratorCreated.WaitOne();

        Logger?.Debug(
            "Event consumer task (api=`{0}', topic=`{1}') started successfully.",
            apiTopic,
            topic);

        return task;
    }

    IEnumerable<KeyValuePair<MethodKey, Method>> RegisterMethods(Api api)
    {
        Logger?.Debug("Searching for methods (api=`{0}').", api.Topic);

        foreach (var itf in api.Interfaces)
        {
            var methods = itf.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods)
            {
                if (method.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault() is CommandAttribute attr)
                {
                    var parameterType = method
                                            .GetParameters()
                                            .SingleOrDefault()
                                            ?.ParameterType
                                        ?? typeof(void);

                    var returnType = method
                                         .ReturnType
                                         .GetGenericArguments()
                                         .SingleOrDefault()
                                     ?? typeof(void);

                    Logger?.Debug(
                        "Found method {0}({1}) (api=`{2}', topic=`{3}').",
                        method.Name,
                        parameterType.Name,
                        api.Topic,
                        attr.Topic);

                    yield return new KeyValuePair<MethodKey, Method>(
                        new MethodKey(api.Topic, attr.Topic),
                        new Method(api.Instance, method, parameterType, returnType));
                }
            }
        }
    }

    async Task ConnectedAsync(EventArgs e)
    {
        Logger?.Info(
            "Node (domain=`{0}', kind=`{1}', id={2}) connected to message broker.",
            Domain,
            Kind,
            Id);

        try
        {
            var nodeDetails = new NodeDetails(Domain, Kind, Id);

            var token = _cancellationTokenSource.Token;

            await _connection.SendStatusAsync(
                new NodeStatusMessage(nodeDetails, ENodeStatus.Connecting),
                token);

            var apis = _apis
                .Select(api => new ApiInfo(api.Topic, api.Version))
                .ToArray();

            var tasks = new List<Task>
            {
                _connection.SendApiInfoAsync(
                    new ApiInfoMessage(nodeDetails, apis),
                    token)
            };

            tasks.AddRange(_apis
                .Select(api => _connection.SubscribeToApiMessagesAsync(
                    new ApiFilter(nodeDetails, api.Topic),
                    EDirection.In,
                    token)));

            await Task.WhenAll(tasks);

            await _connection.SendStatusAsync(
                new NodeStatusMessage(nodeDetails, MapCurrentStatus()),
                token);
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }

        await InvokeConnectionHandlersAsync();
    }

    ENodeStatus MapCurrentStatus()
    {
        lock (_statusLock)
        {
            return MapStatus(_status);
        }
    }

    static ENodeStatus MapStatus(EStatus status)
        => status switch
        {
            EStatus.Ok => ENodeStatus.Online,
            EStatus.Warning => ENodeStatus.Warning,
            EStatus.Error => ENodeStatus.Error,
            _ => throw new ArgumentException()
        };

    Task InvokeConnectionHandlersAsync()
        => Task.Run(() =>
        {
            var l = new List<IConnected>();

            if (this is IConnected self)
            {
                l.Add(self);
            }

            l.AddRange(_apis
                .Select(api => api.Instance)
                .OfType<IConnected>());

            foreach (var c in l)
            {
                try
                {
                    c.Connected();
                }
                catch (Exception ex)
                {
                    Logger?.Warn(ex);
                }
            }
        });

    Task DisconnectedAsync(EventArgs args)
        => Task.Run(() =>
        {
            Logger?.Info(
                "Node (domain=`{0}', kind=`{1}', id={2}) disconnected from message broker.",
                Domain,
                Kind,
                Id);

            var l = new List<IDisconnected>();

            if (this is IDisconnected self)
            {
                l.Add(self);
            }

            l.AddRange(_apis
                .Select(api => api.Instance)
                .OfType<IDisconnected>());

            foreach (var d in l)
            {
                try
                {
                    d.Disconnected();
                }
                catch (Exception ex)
                {
                    Logger?.Warn(ex);
                }
            }
        });

    Task ApiMessageReceivedAsync(ApiMessageEventArgs e)
    {
        Logger?.Debug(
            "Processing message (kind=`{0}', id={1}, api=`{2}', topic=`{3}', size={4}).",
            Kind,
            Id,
            e.Api,
            e.Topic,
            (e is { Payload: not null })
                ? e.Payload.Length
                : 0);

        if (e.Direction.Equals(EDirection.In)
            && e.Node.Domain.Equals(Domain)
            && e.Node.Kind.Equals(Kind)
            && e.Node.Id.Equals(Id)
            && _methods.TryGetValue(new MethodKey(e.Api, e.Topic), out var method))
        {
            try
            {
                var parameters = new List<object?>();

                if (method.ParameterType != typeof(void))
                {
                    object? value = null;

                    if (e.Payload != null)
                    {
                        value = _serializer.Deserialize(e.Payload, method.ParameterType);
                    }

                    parameters.Add(value);
                }

                var task = method.MethodInfo.Invoke(method.Instance, parameters.ToArray()) as Task;

                _pendingCommands.Put(
                    task!,
                    method.ReturnType,
                    e.ResponseTopic,
                    e.MessageId);
            }
            catch (Exception ex)
            {
                Logger?.Error(
                    "Message handler (kind=`{0}', id={1}, api=`{2}', topic=`{3}') failed: {4}",
                    Kind,
                    Id,
                    e.Api,
                    e.Topic,
                    ex.ToString());
            }
        }
        else
        {
            Logger?.Debug(
                "No message handler found (kind=`{0}', id={1}, api=`{2}', topic=`{3}').",
                Kind,
                Id,
                e.Api,
                e.Topic);
        }

        return Task.CompletedTask;
    }

    async Task ProcessPendingTasksAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var completedTasks = await _pendingCommands.WaitAsync(_cancellationTokenSource.Token);

                foreach (var completedTask in completedTasks)
                {
                    if (completedTask.ReturnType != typeof(void)
                        && completedTask is
                        {
                            ResponseTopic: not null,
                            MessageId: not null,
                            Task.IsCompleted: true
                        })
                    {
                        try
                        {
                            var result = completedTask
                                .Task
                                .GetType()
                                .GetProperty("Result")
                                ?.GetValue(completedTask.Task);

                            var message = new ResponseMessage(
                                completedTask.ResponseTopic,
                                completedTask.MessageId.Value,
                                result!);

                            await _connection.SendResponseAsync(message, _cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            Logger?.Error(ex);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // (<>..<>)
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex);

                throw;
            }
        }
    }

    public async Task ShutdownGracefullyAsync()
    {
        if (Interlocked.CompareExchange(ref _shutdown, Yes, No) == Yes)
        {
            throw new InvalidOperationException("Shutdown already in progress or completed.");
        }

        Logger?.Info(
            "Gracefully shutting down client (domain=`{0}', kind=`{1}', id={2}).",
            Domain,
            Kind,
            Id);

        _connection.ApiMessageReceivedAsync -= ApiMessageReceivedAsync;
        _connection.ConnectedAsync -= ConnectedAsync;
        _connection.DisconnectedAsync -= DisconnectedAsync;

        try
        {
            await Task.WhenAll(WaitForPendingEventsAsync(), WaitForPendingTasksAsync());
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }

        try
        {
            Logger?.Debug("Client is offline (domain=`{0}', kind=`{1}', id={2}).", Domain, Kind, Id);

            await _connection.SendStatusAsync(
                new NodeStatusMessage(
                    new NodeDetails(Domain, Kind, Id), ENodeStatus.Offline),
                _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }
    }

    async Task WaitForPendingEventsAsync()
    {
        Logger?.Info(
            "Waiting for last events (domain=`{0}', kind=`{1}', id={2}).",
            Domain,
            Kind,
            Id);

        if (_eventSubscriptions.Any())
        {
            await Task.WhenAll(_eventSubscriptions);
        }
    }

    async Task WaitForPendingTasksAsync()
    {
        Logger?.Debug(
            "Waiting for pending tasks (domain=`{0}', kind=`{1}', id={2}).",
            Domain,
            Kind,
            Id);

        var count = _pendingCommands.Count;

        while (count > 0 && _connection.IsConnected)
        {
            Logger?.Trace(
                "{0} pending task(s) left (domain=`{1}', kind=`{2}', id={3}).",
                count,
                Domain,
                Kind,
                Id);

            await Task.Delay(500);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connection.ConnectedAsync -= ConnectedAsync;
        _connection.DisconnectedAsync -= DisconnectedAsync;
        _connection.ApiMessageReceivedAsync -= ApiMessageReceivedAsync;

        _cancellationTokenSource.Cancel();

        try
        {
            Logger?.Debug(
                "Disposing client, waiting for events (domain=`{0}', kind=`{1}', id={2}).",
                Domain,
                Kind,
                Id);

            if (_eventSubscriptions.Any())
            {
                await Task.WhenAll(_eventSubscriptions);
            }
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }

        try
        {
            Logger?.Debug(
                "Disposing client, cancelling pending tasks (domain=`{0}', kind=`{1}', id={2}).",
                Domain,
                Kind,
                Id);

            if (_pendingCommandsProcessor is not null)
            {
                await _pendingCommandsProcessor;
            }
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }

        try
        {
            if (_connection.IsConnected)
            {
                await _connection.SendStatusAsync(
                        new NodeStatusMessage(
                            new NodeDetails(Domain, Kind, Id), ENodeStatus.Offline));
            }
        }
        catch (Exception ex)
        {
            Logger?.Warn(ex);
        }
    }
}