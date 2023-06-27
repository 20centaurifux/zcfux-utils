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
using System.Diagnostics;
using zcfux.Logging;

namespace zcfux.Telemetry.Discovery;

public sealed class Discoverer : IDisposable
{
    public event Func<EventArgs, Task>? ConnectedAsync;
    public event Func<EventArgs, Task>? DisconnectedAsync;
    public event Func<DiscoveredEventArgs, Task>? DiscoveredAsync;

    static readonly TimeSpan WaitForOnlineStatusTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(30);
    static readonly TimeSpan DanglingQueueTimeout = TimeSpan.FromMinutes(5);

    readonly IConnection _connection;
    readonly IReadOnlyCollection<NodeFilter> _filters;
    readonly ApiRegistry _apiRegistry;
    readonly ISerializer _serializer;
    readonly ILogger? _logger;

    readonly object _discoveredNodesLock = new();
    readonly Dictionary<NodeDetails, DiscoveredNode> _discoveredNodes = new();

    readonly object _apiProxiesLock = new();
    readonly Dictionary<NodeDetails, ApiProxies> _apiProxies = new();

    readonly PendingRequests _pendingRequests;
    readonly ReceivedEvents _receivedEvents;

    const long No = 0;
    const long Yes = 1;

    long _initialized = No;

    Task? _cleanupTask;
    CancellationTokenSource? _cancellationTokenSource;

    bool _disposed;

    public Discoverer(Options options)
    {
        (_connection, _filters, _apiRegistry, _serializer, _logger) = options;

        _pendingRequests = new PendingRequests(_serializer);

        _receivedEvents = new ReceivedEvents();
    }

    public async Task SetupAsync()
    {
        if (Interlocked.CompareExchange(ref _initialized, Yes, No) == Yes)
        {
            throw new InvalidOperationException();
        }

        _connection.ConnectedAsync += ClientConnectedAsync;
        _connection.DisconnectedAsync += ClientDisconnectedAsync;
        _connection.StatusReceivedAsync += StatusReceivedAsync;
        _connection.ApiInfoReceivedAsync += ApiInfoReceivedAsync;
        _connection.ResponseReceivedAsync += ResponseReceivedAsync;
        _connection.ApiMessageReceivedAsync += ApiMessageReceivedAsync;

        _receivedEvents.Received += EventReceived;

        if (_connection.IsConnected)
        {
            await ClientConnectedAsync(EventArgs.Empty);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        _cleanupTask = Task.Run(async () => await CleanupAsync(_cancellationTokenSource.Token));
    }

    async Task ClientConnectedAsync(EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') connected.", _connection.ClientId);

        await Task.WhenAll(_filters.Select(f =>
            _connection.SubscribeToStatusAsync(f)));

        await Task.WhenAll(_filters.Select(f =>
            _connection.SubscribeToApiInfoAsync(f)));

        await Task.WhenAll(_filters.Select(f =>
            _connection.SubscribeToApiMessagesAsync(
                new ApiFilter(f, ApiFilter.All),
                EDirection.Out)));

        if (ConnectedAsync is not null)
        {
            await ConnectedAsync.Invoke(e);
        }
    }

    async Task ClientDisconnectedAsync(EventArgs e)
    {
        _logger?.Debug("Discoverer (client=`{0}') disconnected.", _connection.ClientId);

        if (DisconnectedAsync is not null)
        {
            await DisconnectedAsync.Invoke(e);
        }
    }

    async Task StatusReceivedAsync(NodeStatusEventArgs e)
    {
        _logger?.Debug(
            "Node status received (client=`{0}', domain=`{1}', kind=`{2}', id=`{3}', status={4}).",
            _connection.ClientId,
            e.Node.Domain,
            e.Node.Kind,
            e.Node.Id,
            e.Status);

        var discoveredNode = await LookupOrRegisterNodeAsync(e.Node, e.Status);

        await discoveredNode.ChangeStatusAsync(e.Status);
    }

    async Task<DiscoveredNode> LookupOrRegisterNodeAsync(NodeDetails node, ENodeStatus status)
    {
        DiscoveredNode? discoveredNode = TryGetNode(node);

        if (discoveredNode is null)
        {
            _logger?.Info(
                "Discovered node (client=`{0}', domain=`{1}', kind=`{2}', id=`{3}', status={4}).",
                _connection.ClientId,
                node.Domain,
                node.Kind,
                node.Id,
                status);

            discoveredNode = await RegisterNodeAsync(node, status);
        }

        return discoveredNode;
    }

    DiscoveredNode? TryGetNode(NodeDetails node)
    {
        lock (_discoveredNodesLock)
        {
            _discoveredNodes.TryGetValue(node, out var discoveredNode);

            return discoveredNode;
        }
    }

    async Task<DiscoveredNode> RegisterNodeAsync(NodeDetails node, ENodeStatus status)
    {
        var discoveredNode = new DiscoveredNode(node);

        await discoveredNode.ChangeStatusAsync(status);

        await _connection.SubscribeResponseAsync(discoveredNode.Node);

        lock (_discoveredNodesLock)
        {
            if (!_discoveredNodes.TryAdd(node, discoveredNode))
            {
                _logger?.Warn(
                    "Node (client=`{0}', domain=`{1}', kind=`{2}', id=`{3}') already discovered.",
                    _connection.ClientId,
                    node.Domain,
                    node.Kind,
                    node.Id);

                discoveredNode = _discoveredNodes[node];
            }
        }

        if (DiscoveredAsync is not null)
        {
            await DiscoveredAsync.Invoke(new DiscoveredEventArgs(discoveredNode));
        }

        return discoveredNode;
    }

    async Task ApiInfoReceivedAsync(ApiInfoEventArgs e)
    {
        _logger?.Debug(
            "Discoverer (client=`{0}') received api information (domain=`{1}', kind=`{2}', id=`{3}').",
            _connection.ClientId,
            e.Node.Domain,
            e.Node.Kind,
            e.Node.Id);

        try
        {
            if (TryGetDiscoveredNode(e.Node) is { } discoveredNode)
            {
                var stopwatch = Stopwatch.StartNew();

                while (discoveredNode.Status == ENodeStatus.Offline
                    && stopwatch.Elapsed < WaitForOnlineStatusTimeout)
                {
                    await Task.Delay(250);
                }

                if (discoveredNode.Status != ENodeStatus.Offline)
                {
                    await RegisterApisAsync(discoveredNode, e.Apis);
                }
                else
                {
                    _logger?.Warn(
                        "Couldn't register apis, node (domain=`{0}', kind=`{1}', id={2}) is offline.",
                        e.Node.Domain,
                        e.Node.Kind,
                        e.Node.Id);
                }
            }
            else
            {
                _logger?.Warn(
                    "Couldn't register apis, node (domain=`{0}', kind=`{1}', id={2}) not found.",
                    e.Node.Domain,
                    e.Node.Kind,
                    e.Node.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    DiscoveredNode? TryGetDiscoveredNode(NodeDetails nodeDetails)
    {
        lock (_discoveredNodesLock)
        {
            return _discoveredNodes.GetValueOrDefault(nodeDetails);
        }
    }

    async Task RegisterApisAsync(DiscoveredNode discoveredNode, ApiInfo[] apis)
    {
        var proxies = GetApiProxies(discoveredNode.Node);

        var (dropped, registered) = proxies.Rebuild(apis);

        if (dropped.Any())
        {
            foreach (var proxy in dropped)
            {
                _receivedEvents.Unsubscribe(discoveredNode.Node, proxy.Api.Topic);
            }

            await Task.WhenAll(dropped.Select(discoveredNode.DropApiAsync));
        }

        if (registered.Any())
        {
            foreach (var proxy in registered)
            {
                if (proxy.Instance is IProxy instance)
                {
                    foreach (var (eventTopic, _) in instance.EventTopics)
                    {
                        _receivedEvents.Subscribe(discoveredNode.Node, proxy.Api.Topic, eventTopic);
                    }

                    instance.SendCommandAsync += e =>
                    {
                        _logger?.Debug(
                            "Sending command (api=`{0}', topic=`{1}', ttl={2}) to node (domain=`{3}', kind=`{4}', id={5}).",
                            e.Api,
                            e.Topic,
                            e.TimeToLive,
                            discoveredNode.Node.Domain,
                            discoveredNode.Node.Kind,
                            discoveredNode.Node.Id);

                        return _connection.SendApiMessageAsync(
                            new ApiMessage(
                                discoveredNode.Node,
                                e.Api,
                                e.Topic,
                                new MessageOptions(Retain: false, TimeToLive: e.TimeToLive),
                                EDirection.In,
                                e.Parameter));
                    };

                    instance.SendRequestAsync += e =>
                    {
                        _logger?.Debug(
                            "Sending request (api=`{0}', topic=`{1}', ttl={2}, message id={3}) to node (domain=`{4}', kind=`{5}', id={6}).",
                            e.Api,
                            e.Topic,
                            e.TimeToLive,
                            e.MessageId,
                            discoveredNode.Node.Domain,
                            discoveredNode.Node.Kind,
                            discoveredNode.Node.Id);

                        var pendingRequestTask = _pendingRequests.Add(discoveredNode.Node, e);

                        return _connection
                            .SendApiMessageAsync(
                                new ApiMessage(
                                    discoveredNode.Node,
                                    e.Api,
                                    e.Topic,
                                    new MessageOptions(Retain: false, TimeToLive: e.TimeToLive),
                                    EDirection.In,
                                    e.Parameter,
                                    $"r/{_connection.ClientId}/{discoveredNode.Node.Domain}/{discoveredNode.Node.Kind}/{discoveredNode.Node.Id}",
                                    e.MessageId))
                            .ContinueWith(_ => pendingRequestTask)
                            .Unwrap();
                    };
                }
            }

            await Task.WhenAll(registered.Select(discoveredNode.RegisterApiAsync));
        }
    }

    Task ResponseReceivedAsync(ResponseEventArgs e)
    {
        _pendingRequests.HandleResponseEvent(e);

        return Task.CompletedTask;
    }

    Task ApiMessageReceivedAsync(ApiMessageEventArgs e)
    {
        if (e.Direction == EDirection.Out)
        {
            _receivedEvents.Add(
                e.Node,
                e.Api,
                e.Topic,
                e.Payload,
                e.TimeToLive);
        }

        return Task.CompletedTask;
    }

    void EventReceived(object? sender, ReceivedEventArgs e)
    {
        var proxies = GetApiProxies(e.Node);

        var proxy = proxies.GetProxy(e.Api);

        if (proxy.Instance is IProxy instance)
        {
            var topicType = instance
                .EventTopics
                .SingleOrDefault(t => t.Topic.Equals(e.Topic))
                .Type;

            if (topicType is not null)
            {
                var obj = _serializer.Deserialize(e.Payload, topicType);

                instance.ReceiveEvent(e.Topic, obj!);
            }
        }
    }

    ApiProxies GetApiProxies(NodeDetails nodeDetails)
    {
        ApiProxies? proxies;

        lock (_apiProxiesLock)
        {
            if (!_apiProxies.TryGetValue(nodeDetails, out proxies))
            {
                proxies = new ApiProxies(_apiRegistry);

                _apiProxies[nodeDetails] = proxies;
            }
        }

        return proxies;
    }

    async Task CleanupAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, cancellationToken);

            _receivedEvents.DropDanglingQueues(DanglingQueueTimeout);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection.ConnectedAsync -= ClientConnectedAsync;
                _connection.DisconnectedAsync -= ClientDisconnectedAsync;
                _connection.StatusReceivedAsync -= StatusReceivedAsync;
                _connection.ApiInfoReceivedAsync -= ApiInfoReceivedAsync;
                _connection.ResponseReceivedAsync -= ResponseReceivedAsync;
                _connection.ApiMessageReceivedAsync -= ApiMessageReceivedAsync;

                _receivedEvents.Received -= EventReceived;

                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cleanupTask?.Wait();
                }
                catch
                {
                    //O\\
                }
            }

            _disposed = true;
        }
    }
}