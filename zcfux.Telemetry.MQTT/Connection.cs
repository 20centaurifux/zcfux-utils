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
using System.Text.RegularExpressions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using zcfux.Logging;

namespace zcfux.Telemetry.MQTT;

public sealed class Connection : IConnection
{
    static readonly MqttFactory Factory = new();

    static readonly Regex DeviceStatusRegex = new(
        "^([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)/status$",
        RegexOptions.IgnoreCase);

    static readonly Regex ApiVersionRegex = new(
        "^([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)/apis",
        RegexOptions.IgnoreCase);

    static readonly Regex ApiTopicRegex = new(
        "^([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)/a/([a-z0-9-_]+)/([<|>])/([a-z0-9]+)$",
        RegexOptions.IgnoreCase);

    static readonly Regex ResponseRegex = new(
        "^r/[a-z0-9-_]+/([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)",
        RegexOptions.IgnoreCase);

    static readonly ISerializer Serializer = new Serializer();

    const long Offline = 0;
    const long Online = 1;
    const long GracefulDisconnect = 2;

    readonly ILogger? _logger;

    readonly Func<MqttApplicationMessage, Task<bool>>[] _applicationMessageProcessors;

    readonly CancellationTokenSource _cancellationTokenSource = new();

    readonly MqttClientOptions _clientOptions;
    readonly IMqttClient _client;

    readonly IMessageQueue _messageQueue;

    readonly bool _cleanupRetainedMessages;

    long _connectivity = Offline;

    readonly Task _sendTask;
    readonly TimeSpan _retrySendingInterval;

    readonly TimeSpan? _reconnect;
    readonly object _reconnectLock = new();
    Task? _reconnectTask;
    CancellationTokenSource? _reconnectCancellationTokenSource;

    readonly ConcurrentBag<MqttApplicationMessage> _retainedMessages = new();

    public event Func<EventArgs, Task>? ConnectedAsync;
    public event Func<EventArgs, Task>? DisconnectedAsync;
    public event Func<ApiInfoEventArgs, Task>? ApiInfoReceivedAsync;
    public event Func<ApiMessageEventArgs, Task>? ApiMessageReceivedAsync;
    public event Func<NodeStatusEventArgs, Task>? StatusReceivedAsync;
    public event Func<ResponseEventArgs, Task>? ResponseReceivedAsync;

    public bool IsConnected
        => Interlocked.Read(ref _connectivity) == Online;

    public string ClientId
        => _clientOptions.ClientId;

    public Connection(ConnectionOptions options)
    {
        _logger = options.Logger;

        _applicationMessageProcessors = new[]
        {
            TryProcessDeviceStatusAsync,
            TryProcessApiInfoAsync,
            TryProcessApiMessageAsync,
            TryProcessResponseAsync
        };

        _messageQueue = options.MessageQueue;
        _cleanupRetainedMessages = options.CleanupRetainedMessages;
        _reconnect = options.Reconnect;

        _clientOptions = BuildMqttClientOptions(options.ClientOptions);

        _client = Factory.CreateMqttClient();

        _client.ConnectedAsync += ClientConnectedAsync;
        _client.DisconnectedAsync += ClientDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;

        _retrySendingInterval = options.RetrySendingInterval;
        _sendTask = SendMessagesAsync(_cancellationTokenSource.Token);
    }

    static MqttClientOptions BuildMqttClientOptions(ClientOptions options)
    {
        var builder = Factory.CreateClientOptionsBuilder()
            .WithTcpServer(options.Address, options.Port)
            .WithTimeout(options.Timeout)
            .WithKeepAlivePeriod(options.KeepAlive)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(options.ClientId)
            .WithSessionExpiryInterval(options.SessionTimeout)
            .WithCleanSession(false)
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        if (options.Tls)
        {
            builder = builder.WithTls(o => { o.AllowUntrustedCertificates = options.AllowUntrustedCertificates; });
        }

        if (options.Credentials is { } credentials)
        {
            builder = builder
                .WithCredentials(credentials.Username, credentials.Password);
        }

        if (options.LastWill is { } lwt)
        {
            builder = builder
                .WithWillTopic($"{lwt.Node.Domain}/{lwt.Node.Kind}/{lwt.Node.Id}/status")
                .WithWillPayload(Convert.ToInt32(ENodeStatus.Offline).ToString())
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithWillRetain(lwt.MessageOptions.Retain);
        }

        return builder.Build();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Connecting client `{0}'.", ClientId);

        var waitForConnectedEventTask = WaitForConnectedEventAsync();

        await Task.WhenAll(
            waitForConnectedEventTask,
            _client.ConnectAsync(_clientOptions, cancellationToken));
    }

    async Task WaitForConnectedEventAsync()
    {
        var tcs = new TaskCompletionSource();

        Task Handler(MqttClientConnectedEventArgs _)
        {
            tcs.SetResult();

            return Task.CompletedTask;
        }

        _client.ConnectedAsync += Handler;

        try
        {
            await tcs.Task;
        }
        finally
        {
            _client.ConnectedAsync -= Handler;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Disconnecting client `{0}'.", ClientId);

        while (_messageQueue.Count > 0)
        {
            _logger?.Trace("Client `{0}' messages in backlog: {1}", ClientId, _messageQueue.Count);

            await Task.Delay(500, cancellationToken);
        }

        _logger?.Debug("Client `{0}' cancels pending tasks and deletes retained messages.", ClientId);

        await Task.WhenAny(CancelReconnectAsync(), DeleteRetainedMessagesAsync());

        Interlocked.Exchange(ref _connectivity, GracefulDisconnect);

        if (!string.IsNullOrEmpty(_clientOptions.WillTopic))
        {
            _logger?.Debug("Client `{0}' sends last will.", ClientId);

            var builder = new MqttApplicationMessageBuilder()
                .WithTopic(_clientOptions.WillTopic)
                .WithPayload(_clientOptions.WillPayload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(_clientOptions.WillRetain);

            var lastWillMessage = builder.Build();

            await _client.PublishAsync(lastWillMessage, cancellationToken);
        }

        await CloseClientConnectionAsync(cancellationToken);
    }

    async Task CloseClientConnectionAsync(CancellationToken cancellationToken)
    {
        var opts = Factory.CreateClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectOptionsReason.DisconnectWithWillMessage)
            .Build();

        var tcs = new TaskCompletionSource();

        Task Handler(MqttClientDisconnectedEventArgs _)
        {
            tcs.SetResult();

            return Task.CompletedTask;
        }

        _client.DisconnectedAsync += Handler;

        try
        {
            await Task.WhenAll(
                _client.DisconnectAsync(opts, cancellationToken),
                tcs.Task);
        }
        finally
        {
            _client.DisconnectedAsync -= Handler;
        }
    }

    async Task DeleteRetainedMessagesAsync()
    {
        if (IsConnected)
        {
            if (_retainedMessages.Any())
            {
                await Task.WhenAll(_retainedMessages.Select(msg =>
                    _client.PublishAsync(msg, CancellationToken.None)));
            }
        }
    }

    async Task ClientConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' connected successfully.", ClientId);

        Interlocked.Exchange(ref _connectivity, Online);

        try
        {
            if (ConnectedAsync is not null)
            {
                await ConnectedAsync(EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    async Task SendMessagesAsync(CancellationToken cancellationToken)
    {
        MqttApplicationMessage? message = null;
        TimeSpan? expiryInterval = null;
        Stopwatch stopwatch = new();

        for (; ; )
        {
            try
            {
                if (message != null
                    && expiryInterval.HasValue
                    && stopwatch.Elapsed > expiryInterval.Value)
                {
                    _logger?.Debug("Message expired (client=`{0}').", ClientId);

                    message = null;
                }

                if (message == null)
                {
                    _logger?.Debug("Client `{0}' is waiting for messages to send.", ClientId);

                    message = await _messageQueue.DequeueAsync(cancellationToken);

                    if (message.MessageExpiryInterval == 0)
                    {
                        expiryInterval = null;
                    }
                    else
                    {
                        expiryInterval = TimeSpan.FromSeconds(message.MessageExpiryInterval);

                        stopwatch.Restart();
                    }
                }

                if (_client.IsConnected)
                {
                    _logger?.Debug(
                        "Client `{0}' sends message to topic `{1}' (size={2}).",
                        ClientId,
                        message.Topic,
                        message.PayloadSegment.Count);

                    if (_logger?.Verbosity == ESeverity.Trace)
                    {
                        _logger?.Trace(
                            "Sending message (client=`{0}', topic=`{1}'): `{2}'",
                            ClientId,
                            message.Topic,
                            message.ConvertPayloadToString());
                    }

                    await _client.PublishAsync(message, cancellationToken);

                    message = null;
                }
                else
                {
                    _logger?.Debug("Client `{0}' is disconnected, cannot send messages.", ClientId);

                    await Task.Delay(_retrySendingInterval, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger?.Warn(ex);

                await Task.Delay(_retrySendingInterval, cancellationToken);
            }
        }
    }

    async Task ClientDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' disconnected.", ClientId);

        var previousStatus = Interlocked.Exchange(ref _connectivity, Offline);

        try
        {
            if (DisconnectedAsync is not null)
            {
                await DisconnectedAsync(EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        await InitializeReconnectAsync(previousStatus);
    }

    async Task InitializeReconnectAsync(long previousStatus)
    {
        if (_reconnect.HasValue)
        {
            if (previousStatus != GracefulDisconnect)
            {
                lock (_reconnectLock)
                {
                    if (_reconnectCancellationTokenSource == null
                        && _reconnectTask == null)
                    {
                        _reconnectCancellationTokenSource = new CancellationTokenSource();

                        var token = _reconnectCancellationTokenSource.Token;

                        _logger?.Debug(
                            "Reconnecting client `{0}' in {1}s.",
                            ClientId,
                            _reconnect.Value.TotalSeconds);

                        _reconnectTask = Task.Run(async () =>
                        {
                            await Task.Delay(_reconnect.Value, token);

                            lock (_reconnectLock)
                            {
                                _reconnectCancellationTokenSource = null;
                                _reconnectTask = null;
                            }

                            return ConnectAsync(token);
                        }, token);
                    }
                }
            }
            else
            {
                await CancelReconnectAsync();
            }
        }
    }

    async Task CancelReconnectAsync()
    {
        CancellationTokenSource? cancellationTokenSource = null;
        Task? task = null;

        lock (_reconnectLock)
        {
            if (_reconnectCancellationTokenSource != null)
            {
                cancellationTokenSource = _reconnectCancellationTokenSource;
                task = _reconnectTask;

                _reconnectCancellationTokenSource = null;
                _reconnectTask = null;
            }
        }

        if (cancellationTokenSource != null
            && task != null)
        {
            cancellationTokenSource.Cancel();

            await task;
        }
    }

    async Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        if (_logger?.Verbosity == ESeverity.Trace)
        {
            _logger?.Trace(
                "Client `{0}' received message in `{1}' (size={2}): `{3}'",
                ClientId,
                e.ApplicationMessage.Topic,
                e.ApplicationMessage.PayloadSegment.Count,
                (e.ApplicationMessage.PayloadSegment.Count > 0)
                    ? e.ApplicationMessage.ConvertPayloadToString()
                    : string.Empty);
        }

        try
        {
            foreach (var p in _applicationMessageProcessors)
            {
                if (await p(e.ApplicationMessage))
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    async Task<bool> TryProcessDeviceStatusAsync(MqttApplicationMessage message)
    {
        var success = false;

        if (message.PayloadSegment.Any())
        {
            var m = DeviceStatusRegex.Match(message.Topic);

            if (m.Success && StatusReceivedAsync is not null)
            {
                try
                {
                    await StatusReceivedAsync(new NodeStatusEventArgs(
                        new NodeDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        Serializer.Deserialize<ENodeStatus>(message.PayloadSegment.ToArray())));
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }

            success = m.Success;
        }

        return success;
    }

    async Task<bool> TryProcessApiInfoAsync(MqttApplicationMessage message)
    {
        var success = false;

        if (message.PayloadSegment.Any())
        {
            var m = ApiVersionRegex.Match(message.Topic);

            if (m.Success && ApiInfoReceivedAsync is not null)
            {
                try
                {
                    await ApiInfoReceivedAsync(new ApiInfoEventArgs(
                        new NodeDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        Serializer.Deserialize<ApiInfo[]>(message.PayloadSegment.ToArray())!));
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }

            success = m.Success;
        }

        return success;
    }

    async Task<bool> TryProcessApiMessageAsync(MqttApplicationMessage message)
    {
        var m = ApiTopicRegex.Match(message.Topic);

        if (m.Success)
        {
            string? responseTopic = null;

            if (!string.IsNullOrEmpty(message.ResponseTopic))
            {
                responseTopic = message.ResponseTopic;
            }

            int? messageId = null;

            if (message.CorrelationData is { Length: 4 })
            {
                var bytes = message.CorrelationData;

                if (BitConverter.IsLittleEndian)
                {
                    bytes = bytes
                        .Reverse()
                        .ToArray();
                }

                messageId = BitConverter.ToInt32(bytes);
            }

            if (ApiMessageReceivedAsync is not null)
            {
                try
                {
                    await ApiMessageReceivedAsync(new ApiMessageEventArgs(
                        new NodeDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        m.Groups[4].Value,
                        m.Groups[6].Value,
                        message.PayloadSegment.ToArray(),
                        (m.Groups[5].Value == ">")
                            ? EDirection.Out
                            : EDirection.In,
                        TimeSpan.FromSeconds(message.MessageExpiryInterval),
                        responseTopic,
                        messageId));
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex);
                }
            }
        }

        return m.Success;
    }

    public Task SubscribeToStatusAsync(NodeFilter filter, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = filter;

        return _client.SubscribeAsync(
            $"{domain}/{kind}/{id}/status",
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    async Task<bool> TryProcessResponseAsync(MqttApplicationMessage message)
    {
        var success = false;

        if (message.PayloadSegment.Any()
            && message.CorrelationData is { Length: 4 })
        {
            var m = ResponseRegex.Match(message.Topic);

            if (m.Success)
            {
                var bytes = message.CorrelationData;

                if (BitConverter.IsLittleEndian)
                {
                    bytes = bytes
                        .Reverse()
                        .ToArray();
                }

                var messageId = BitConverter.ToInt32(bytes);

                if (ResponseReceivedAsync is not null)
                {
                    try
                    {
                        await ResponseReceivedAsync(new ResponseEventArgs(
                            new NodeDetails(
                                m.Groups[1].Value,
                                m.Groups[2].Value,
                                Convert.ToInt32(m.Groups[3].Value)),
                            messageId,
                            message.PayloadSegment.ToArray()));
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex);
                    }
                }
            }

            success = m.Success;
        }

        return success;
    }

    public Task SendStatusAsync(NodeStatusMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), status) = message;

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/status")
            .WithPayload(Convert.ToInt32(status).ToString())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        return _client.PublishAsync(mqttMessage, cancellationToken);
    }

    public Task SubscribeToApiInfoAsync(NodeFilter filter, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = filter;

        return _client.SubscribeAsync(
            $"{domain}/{kind}/{id}/apis",
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    public async Task SendApiInfoAsync(ApiInfoMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), apis) = message;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/apis")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag();

        var json = Serializer.Serialize(apis);

        await _client.PublishAsync(
            builder
                .WithPayload(json)
                .Build(),
            cancellationToken);

        if (_cleanupRetainedMessages)
        {
            _retainedMessages.Add(
                builder
                    .WithPayload(Array.Empty<byte>())
                    .Build());
        }
    }

    public Task SubscribeToApiMessagesAsync(ApiFilter filter, EDirection direction, CancellationToken cancellationToken)
    {
        var (domain, kind, id, api) = filter;

        var topic = $"{domain}/{kind}/{id}/a/{api}/{((direction == EDirection.In) ? "<" : ">")}/+";

        return _client.SubscribeAsync(
            topic,
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    public Task SendApiMessageAsync(ApiMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), api, topic, options, direction, payload, _, _) = message;

        var json = Serializer.Serialize(payload);

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/a/{api}/{((direction == EDirection.In) ? "<" : ">")}/{topic}")
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(options.Retain)
            .WithMessageExpiryInterval(options.TimeToLive);

        if (message.ResponseTopic is not null)
        {
            builder = builder.WithResponseTopic(message.ResponseTopic);
        }

        if (message.MessageId.HasValue)
        {
            var bytes = BitConverter.GetBytes(message.MessageId.Value);

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes
                    .Reverse()
                    .ToArray();
            }

            builder = builder.WithCorrelationData(bytes);
        }

        var mqttMessage = builder.Build();

        return _messageQueue.EnqueueAsync(mqttMessage, message.Options.TimeToLive, cancellationToken);
    }

    public Task SubscribeResponseAsync(NodeDetails node, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = node;

        var topic = $"r/{ClientId}/{domain}/{kind}/{id}";

        return _client.SubscribeAsync(
            topic,
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    public Task SendResponseAsync(ResponseMessage message, CancellationToken cancellationToken)
    {
        var (topic, messageId, payload) = message;

        var json = Serializer.Serialize(payload);

        var correlationData = BitConverter.GetBytes(messageId);

        if (BitConverter.IsLittleEndian)
        {
            correlationData = correlationData
                .Reverse()
                .ToArray();
        }

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithCorrelationData(correlationData)
            .Build();

        return _messageQueue.EnqueueAsync(mqttMessage, 60 * 5, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await DeleteRetainedMessagesAsync();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        CancelSendTask();

        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger?.Warn(ex);
        }

        _client.Dispose();
    }

    void CancelSendTask()
    {
        try
        {
            _cancellationTokenSource.Cancel();

            Task.WhenAny(_sendTask).Wait();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }
}