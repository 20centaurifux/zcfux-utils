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
    sealed record Api(Type Type, object Instance, string Topic, string Version);
    sealed record Method(object Instance, MethodInfo MethodInfo, Type ParameterType);

    readonly CancellationTokenSource _cancellationTokenSource = new();

    readonly IReadOnlyCollection<Api> _apis;
    readonly IReadOnlyCollection<Task> _subscriptions;
    readonly IReadOnlyDictionary<string, Method> _methods;

    readonly IConnection _connection;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    string Domain { get; }

    string Kind { get; }

    int Id { get; }

    protected Client(Options options)
    {
        ((Domain, Kind, Id), _connection, _serializer, _logger) = options;

        (_apis, _subscriptions, _methods) = RegisterApis();

        if (_connection.IsConnected)
        {
            ClientConnected();
        }

        _connection.Connected += Connected;
        _connection.Disconnected += Disconnected;
        _connection.ApiMessageReceived += MessageReceived;
    }

    (IReadOnlyCollection<Api>, IReadOnlyCollection<Task>, IReadOnlyDictionary<string, Method>) RegisterApis()
    {
        var apis = new List<Api>();
        var subscriptions = new List<Task>();
        var methods = new Dictionary<string, Method>();

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
                    attr.Version);

                apis.Add(api);

                subscriptions.AddRange(SubscribeToAsyncEnumerables(api));

                foreach (var (key, method) in RegisterMethods(api))
                {
                    methods.Add(key, method);
                }
            }
        }

        _logger?.Debug("Device apis disovery completed.");

        return (apis, subscriptions, methods);
    }

    IEnumerable<Task> SubscribeToAsyncEnumerables(Api api)
    {
        _logger?.Debug("Searching for asynchronous enumerables (api=`{0}').", api.Topic);

        var props = api.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in props)
        {
            if (prop.GetCustomAttributes(typeof(OutAttribute), false).SingleOrDefault() is OutAttribute attr)
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
                        attr.TimeToLive.TotalSeconds);

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

        var taskReadsFromEnumerator = new TaskCompletionSource();

        var task = Task.Run(async () =>
        {
            var enumerator = source.GetAsyncEnumerator(_cancellationTokenSource.Token);

            var moveNextTask = enumerator.MoveNextAsync();

            taskReadsFromEnumerator.SetResult();

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
                            current);

                        await _connection.SendApiMessageAsync(message, _cancellationTokenSource.Token);
                    }

                    completed = !await enumerator.MoveNextAsync();
                }
            }
        });

        _logger?.Debug(
            "Waiting for asynchronous enumerable task (api=`{0}', topic=`{1}') to start.",
            apiTopic,
            topic);

        taskReadsFromEnumerator.Task.Wait();

        _logger?.Debug(
            "Asynchronous enumerable task (api=`{0}', topic=`{1}') started successfully.",
            apiTopic,
            topic);

        return task;
    }

    IEnumerable<KeyValuePair<string, Method>> RegisterMethods(Api api)
    {
        _logger?.Debug("Searching for methods (api=`{0}').", api.Topic);

        var methods = api.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        foreach (var method in methods)
        {
            if (method.GetCustomAttributes(typeof(InAttribute), false).SingleOrDefault() is InAttribute attr)
            {
                var parameterType = method
                    .GetParameters()
                    .Single()
                    .ParameterType;

                _logger?.Debug(
                    "Found method {0}({1}) (api=`{2}', topic=`{3}').",
                    method.Name,
                    parameterType.Name,
                    api.Topic,
                    attr.Topic);

                yield return new KeyValuePair<string, Method>(
                    $"{api.Topic}/{attr.Topic}",
                    new Method(
                        api.Instance,
                        method,
                        parameterType));
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
                .Wait();

            var tasks = _apis
                .Select(api => _connection
                    .SendApiInfoAsync(new ApiInfoMessage(device, api.Topic, api.Version), token))
                .ToArray();

            Task.WaitAll(tasks);

            _connection
                .SendDeviceStatusAsync(new DeviceStatusMessage(device, EDeviceStatus.Online), token)
                .Wait();
        }
        catch (Exception ex)
        {
            _logger?.Warn(ex);
        }

        InvokeConnectionHandlers();
    }

    void InvokeConnectionHandlers()
    {
        foreach (var api in _apis)
        {
            if (api.Instance is IConnected c)
            {
                try
                {
                    c.Connected();
                }
                catch (Exception ex)
                {
                    _logger?.Warn(ex);
                }
            }
        }
    }

    void Disconnected(object? sender, EventArgs args)
    {
        _logger?.Info("Device (kind=`{0}', id={1}) disconnected from message broker.", Kind, Id);

        foreach (var api in _apis)
        {
            if (api.Instance is IDisconnected c)
            {
                try
                {
                    c.Disconnected();
                }
                catch (Exception ex)
                {
                    _logger?.Warn(ex);
                }
            }
        }
    }

    void MessageReceived(object? sender, ApiMessageEventArgs e)
    {
        _logger?.Debug(
            "Message received (device kind=`{0}', id={1}, api=`{2}', topic=`{3}', payload size={4}).",
            Kind,
            Id,
            e.Api,
            e.Topic,
            (e.Payload is { })
                ? e.Payload.Length
                : 0);

        if (e.Payload is { }
            && _methods.TryGetValue($"{e.Api}/{e.Topic}", out var method))
        {
            try
            {
                var parameter = _serializer.Deserialize(e.Payload, method.ParameterType);

                method.MethodInfo.Invoke(method.Instance, new[] { parameter });
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

    public virtual void Dispose()
    {
        _connection.Connected -= Connected;
        _connection.Disconnected -= Disconnected;
        _connection.ApiMessageReceived -= MessageReceived;

        _cancellationTokenSource.Cancel();

        Task.WhenAll(_subscriptions.ToArray());
    }
}