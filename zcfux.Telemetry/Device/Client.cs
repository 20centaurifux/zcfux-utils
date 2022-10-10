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
using zcfux.Logging;

namespace zcfux.Telemetry.Device;

public class Client : IDisposable
{
    sealed record Api(Type Type, object Instance, string Topic, string Version, Type[] Interfaces);

    sealed class MethodKey
    {
        public string Api { get; }

        public string Topic { get; }

        public MethodKey(string api, string topic)
            => (Api, Topic) = (api, topic);

        public override string ToString()
            => $"{Api}/{Topic}";

        public override int GetHashCode()
            => ToString().GetHashCode();

        public override bool Equals(object? obj)
        {
            var equals = false;

            if (obj is MethodKey other)
            {
                equals = ToString().Equals(other.ToString());
            }

            return equals;
        }
    }

    sealed record Method(object Instance, MethodInfo MethodInfo, Type ParameterType, Type ReturnType);

    sealed record PendingTask(Task Task, Type ReturnType, string? ResponseTopic, int? MessageId);

    sealed class PendingTasks
    {
        readonly object _lock = new();
        readonly List<PendingTask> _tasks = new();
        readonly AutoResetEvent _event = new(false);

        public void Put(PendingTask pendingTask)
        {
            lock (_tasks)
            {
                _tasks.Add(pendingTask);
                _event.Set();
            }
        }

        public Task<PendingTask> WaitAsync(CancellationToken cancellationToken)
        {
            PendingTask? pendingTask = null;

            while (pendingTask == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                pendingTask = WaitForTask(cancellationToken);

                if (pendingTask == null)
                {
                    _event.WaitOne(500);
                }
            }

            return Task.FromResult(pendingTask);
        }

        PendingTask? WaitForTask(CancellationToken cancellationToken)
        {
            PendingTask? pendingTask = null;

            var snapshot = GetSnapshot();

            if (snapshot.Any())
            {
                var index = Task.WaitAny(
                    snapshot.Select(t => t.Task).ToArray(),
                    500,
                    cancellationToken);

                if (index != -1)
                {
                    pendingTask = snapshot[index];

                    RemovePendingTaskAt(index);
                }
            }

            return pendingTask;
        }

        PendingTask[] GetSnapshot()
        {
            lock (_lock)
            {
                return _tasks.ToArray();
            }
        }

        void RemovePendingTaskAt(int index)
        {
            lock (_lock)
            {
                _tasks.RemoveAt(index);
            }
        }
    }

    readonly CancellationTokenSource _cancellationTokenSource = new();

    readonly IReadOnlyCollection<Api> _apis;
    readonly IReadOnlyCollection<Task> _subscriptions;
    readonly IReadOnlyDictionary<MethodKey, Method> _methods;

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly PendingTasks _pendingTasks = new();

    readonly Task _pendingTaskProcessor;

    string Domain { get; }

    string Kind { get; }

    int Id { get; }

    protected Client(Options options)
    {
        ((Domain, Kind, Id), _connection, _serializer, _logger) = options;

        (_apis, _subscriptions, _methods) = RegisterApis();

        _connection.Connected += Connected;
        _connection.Disconnected += Disconnected;
        _connection.ApiMessageReceived += ApiMessageReceived;

        _pendingTaskProcessor = ProcessPendingTasksAsync();

        if (_connection.IsConnected)
        {
            ClientConnected();
        }
    }

    (IReadOnlyCollection<Api>, IReadOnlyCollection<Task>, IReadOnlyDictionary<MethodKey, Method>) RegisterApis()
    {
        var apis = new List<Api>();
        var subscriptions = new List<Task>();
        var methods = new Dictionary<MethodKey, Method>();

        _logger?.Debug("Discovering device (kind=`{0}', id={1}) apis.", Kind, Id);

        foreach (var prop in GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (prop
                    .PropertyType
                    .GetCustomAttributes(typeof(ApiAttribute), false)
                    .SingleOrDefault() is ApiAttribute attr)
            {
                _logger?.Debug(
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

        _logger?.Debug("Device apis discovery completed.");

        return (apis, subscriptions, methods);
    }

    IEnumerable<Task> SubscribeToAsyncEnumerables(Api api)
    {
        _logger?.Debug("Searching for asynchronous enumerables (api=`{0}').", api.Topic);

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

                    _logger?.Debug(
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
        _logger?.Debug(
            "Starting asynchronous enumerable task (api=`{0}', topic=`{1}').",
            apiTopic,
            topic);

        var enumeratorCreated = new AutoResetEvent(false);

        var task = Task.Factory.StartNew(async () =>
        {
            try
            {
                await using (var enumerator = source.GetAsyncEnumerator(_cancellationTokenSource.Token))
                {
                    enumeratorCreated.Set();

                    var moveNextTask = enumerator.MoveNextAsync();

                    if (await moveNextTask)
                    {
                        var completed = false;

                        while (!completed)
                        {
                            var current = enumerator.Current;

                            _logger?.Trace(
                                "Received asynchronous enumerable value (api=`{0}', topic=`{1}', type={2}).",
                                apiTopic,
                                topic,
                                typeof(T).Name);

                            if (current != null)
                            {
                                var message = new ApiMessage(
                                    new DeviceDetails(Domain, Kind, Id),
                                    apiTopic,
                                    topic,
                                    options,
                                    EDirection.Out,
                                    current);

                                await _connection.SendApiMessageAsync(message, _cancellationTokenSource.Token);
                            }

                            completed = !await enumerator.MoveNextAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }, TaskCreationOptions.LongRunning);

        _logger?.Debug(
            "Waiting for asynchronous enumerable task (api=`{0}', topic=`{1}') to start.",
            apiTopic,
            topic);

        enumeratorCreated.WaitOne();

        _logger?.Debug(
            "Asynchronous enumerable task (api=`{0}', topic=`{1}') started successfully.",
            apiTopic,
            topic);

        return task;
    }

    IEnumerable<KeyValuePair<MethodKey, Method>> RegisterMethods(Api api)
    {
        _logger?.Debug("Searching for methods (api=`{0}').", api.Topic);

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

                    _logger?.Debug(
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

    void Connected(object? sender, EventArgs args)
        => ClientConnected();

    void ClientConnected()
    {
        _logger?.Info("Device (kind=`{0}', id={1}) connected to message broker.", Kind, Id);

        try
        {
            var device = new DeviceDetails(Domain, Kind, Id);

            var token = _cancellationTokenSource.Token;

            _connection
                .SendDeviceStatusAsync(new DeviceStatusMessage(device, EDeviceStatus.Connecting), token)
                .Wait(token);

            var tasks = _apis
                .Select(api => _connection
                    .SendApiInfoAsync(new ApiInfoMessage(device, api.Topic, api.Version), token))
                .ToList();

            tasks.AddRange(_apis
                .Select(api => _connection
                    .SubscribeToApiMessagesAsync(device, api.Topic, EDirection.In, token)));

            Task.WaitAll(tasks.ToArray());

            _connection
                .SendDeviceStatusAsync(new DeviceStatusMessage(device, EDeviceStatus.Online), token)
                .Wait(token);
        }
        catch (Exception ex)
        {
            _logger?.Warn(ex);
        }

        InvokeConnectionHandlers();
    }

    void InvokeConnectionHandlers()
    {
        var l = new List<IConnected>();

        if (this is IConnected c1)
        {
            l.Add(c1);
        }

        l.AddRange(_apis
            .Select(api => api.Instance)
            .OfType<IConnected>());

        foreach (var c2 in l)
        {
            try
            {
                c2.Connected();
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex);
            }
        }
    }

    void Disconnected(object? sender, EventArgs args)
    {
        _logger?.Info("Device (kind=`{0}', id={1}) disconnected from message broker.", Kind, Id);

        var l = new List<IDisconnected>();

        if (this is IDisconnected d1)
        {
            l.Add(d1);
        }

        l.AddRange(_apis
            .Select(api => api.Instance)
            .OfType<IDisconnected>());

        foreach (var d2 in l)
        {
            try
            {
                d2.Disconnected();
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex);
            }
        }
    }

    void ApiMessageReceived(object? sender, ApiMessageEventArgs e)
    {
        _logger?.Debug(
            "Processing message (device kind=`{0}', id={1}, api=`{2}', topic=`{3}', size={4}).",
            Kind,
            Id,
            e.Api,
            e.Topic,
            (e is { Payload: { } })
                ? e.Payload.Length
                : 0);

        if (e.Direction.Equals(EDirection.In)
            && e.Device.Domain.Equals(Domain)
            && e.Device.Kind.Equals(Kind)
            && e.Device.Id.Equals(Id)
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

                var pendingTask = new PendingTask(
                    task!,
                    method.ReturnType,
                    e.ResponseTopic,
                    e.MessageId);

                _pendingTasks.Put(pendingTask);
            }
            catch (Exception ex)
            {
                _logger?.Error(
                    "Message handler (device kind=`{0}', id={1}, api=`{2}', topic=`{3}') failed: {4}",
                    Kind,
                    Id,
                    e.Api,
                    e.Topic,
                    ex.ToString());
            }
        }
        else
        {
            _logger?.Debug(
                "No message handler found (device kind=`{0}', id={1}, api=`{2}', topic=`{3}').",
                Kind,
                Id,
                e.Api,
                e.Topic);
        }
    }

    Task ProcessPendingTasksAsync()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var pendingTask = await _pendingTasks.WaitAsync(_cancellationTokenSource.Token);

                if (pendingTask.ReturnType != typeof(void)
                    && pendingTask.ResponseTopic is { }
                    && pendingTask.MessageId.HasValue
                    && pendingTask.Task.IsCompleted)
                {
                    try
                    {
                        var result = pendingTask
                            .Task
                            .GetType()
                            .GetProperty("Result")
                            ?.GetValue(pendingTask.Task);

                        var message = new ResponseMessage(
                            pendingTask.ResponseTopic,
                            pendingTask.MessageId.Value,
                            result!);

                        await _connection.SendResponseAsync(message, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex);
                    }
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    public virtual void Dispose()
    {
        _connection.Connected -= Connected;
        _connection.Disconnected -= Disconnected;
        _connection.ApiMessageReceived -= ApiMessageReceived;

        _cancellationTokenSource.Cancel();

        if (_subscriptions.Any())
        {
            Task.WhenAny(_subscriptions.ToArray()).Wait();
        }

        Task.WhenAny(_pendingTaskProcessor).Wait();
    }
}