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
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using zcfux.Logging;

namespace zcfux.Telemetry.MQTT;

public sealed class Connection : IConnection
{
    static readonly MqttFactory Factory = new();

    static readonly Regex DeviceStatusRegex = new(
        "^([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)/status$",
        RegexOptions.IgnoreCase);

    static readonly Regex ApiVersionRegex = new(
        "^([a-z0-9-_]+)/([a-z0-9-_]+)/([0-9]+)/a/([a-z0-9-_]+)/version$",
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
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly MqttClientOptions _clientOptions;
    readonly IMqttClient _client;

    readonly IMessageQueue _messageQueue;

    readonly bool _cleanupRetainedMessages;

    long _connectivity = Offline;
    readonly ManualResetEvent _onlineEvent = new(false);
    readonly AutoResetEvent _offlineEvent = new(false);

    readonly Task _sendTask;
    readonly TimeSpan _retrySendingInterval;

    readonly TimeSpan? _reconnect;
    readonly object _reconnectLock = new();
    Task? _reconnectTask;
    CancellationTokenSource? _reconnectCancellationTokenSource;

    readonly ConcurrentBag<MqttApplicationMessage> _retainedMessages = new();

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<ApiInfoEventArgs>? ApiInfoReceived;
    public event EventHandler<ApiMessageEventArgs>? ApiMessageReceived;
    public event EventHandler<DeviceStatusEventArgs>? DeviceStatusReceived;
    public event EventHandler<ResponseEventArgs>? ResponseReceived;

    public bool IsConnected
        => Interlocked.Read(ref _connectivity) == Online;

    public string ClientId
        => _clientOptions.ClientId;

    public Connection(ConnectionOptions options)
    {
        _logger = options.Logger;
        _messageQueue = options.MessageQueue;
        _cleanupRetainedMessages = options.CleanupRetainedMessages;
        _reconnect = options.Reconnect;

        _clientOptions = BuildMqttClientOptions(options.ClientOptions);

        _client = Factory.CreateMqttClient();

        _client.ConnectedAsync += ClientConnected;
        _client.DisconnectedAsync += ClientDisconnected;
        _client.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;

        _retrySendingInterval = options.RetrySendingInterval;
        _sendTask = SendMessagesAsync();
    }

    static MqttClientOptions BuildMqttClientOptions(ClientOptions options)
    {
        var builder = Factory.CreateClientOptionsBuilder()
            .WithTcpServer(options.Address, options.Port)
            .WithTimeout(options.Timeout)
            .WithKeepAlivePeriod(options.KeepAlive)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithClientId(options.ClientId)
            .WithSessionExpiryInterval(options.SessionTimeout)
            .WithCleanSession(false)
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        if (options.Tls)
        {
            builder = builder.WithTls(o =>
            {
                o.AllowUntrustedCertificates = options.AllowUntrustedCertificates;
            });
        }

        if (options.Credentials is { } credentials)
        {
            builder = builder
                .WithCredentials(credentials.Username, credentials.Password);
        }

        if (options.LastWill is { } lwt)
        {
            builder = builder
                .WithWillTopic($"{lwt.Device.Domain}/{lwt.Device.Kind}/{lwt.Device.Id}/status")
                .WithWillPayload(Convert.ToInt32(EDeviceStatus.Offline).ToString())
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithWillRetain(lwt.MessageOptions.Retain);
        }

        return builder.Build();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Connecting client `{0}'.", ClientId);

        await _client.ConnectAsync(_clientOptions, cancellationToken);

        _onlineEvent.WaitOne();
        _onlineEvent.Reset();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Disconnecting client `{0}'.", ClientId);

        await Task.WhenAny(CancelReconnectAsync(), DeleteRetainedMessagesAsync());

        Interlocked.Exchange(ref _connectivity, GracefulDisconnect);

        var opts = Factory.CreateClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectReason.DisconnectWithWillMessage)
            .Build();

        await _client.DisconnectAsync(opts, cancellationToken);

        _offlineEvent.WaitOne();
    }

    async Task DeleteRetainedMessagesAsync()
    {
        if (IsConnected)
        {
            var tasks = _retainedMessages
                .Select(msg => _client.PublishAsync(msg, CancellationToken.None))
                .ToArray();

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }
    }

    Task ClientConnected(MqttClientConnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' connected successfully.", ClientId);

        Interlocked.Exchange(ref _connectivity, Online);

        try
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        _onlineEvent.Set();

        return Task.CompletedTask;
    }

    Task SendMessagesAsync()
    {
        var task = Task.Factory.StartNew(async () =>
        {
            MqttApplicationMessage? message = null;
            TimeSpan? expiryInterval = null;
            Stopwatch stopwatch = new();

            while (!_cancellationTokenSource.IsCancellationRequested)
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

                        message = await _messageQueue.DequeueAsync(_cancellationTokenSource.Token);

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

                    if (_client.IsConnected
                        || _onlineEvent.WaitOne(TimeSpan.FromSeconds(1)))
                    {
                        _logger?.Debug(
                            "Client `{0}' sends message to topic `{1}' (size={2}).",
                            ClientId,
                            message.Topic,
                            message.Payload.Length);

                        _logger?.Trace(
                            "Sending message (client=`{0}', topic=`{1}'): `{2}'",
                            ClientId,
                            message.Topic,
                            message.ConvertPayloadToString());

                        await _client.PublishAsync(message, _cancellationTokenSource.Token);

                        message = null;
                    }
                    else
                    {
                        _logger?.Debug("Client `{0}' is disconnected, cannot send messages.", ClientId);

                        await Task.Delay(_retrySendingInterval, _cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warn(ex);
                    
                    await Task.Delay(_retrySendingInterval, _cancellationTokenSource.Token);
                }
            }
        }, TaskCreationOptions.LongRunning);

        return task;
    }

    Task ClientDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' disconnected.", ClientId);

        var previousStatus = Interlocked.Exchange(ref _connectivity, Offline);

        try
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        _offlineEvent.Set();

        return InitializeReconnectAsync(previousStatus);
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

    Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        _logger?.Trace(
            "Client `{0}' received message in `{1}' (size={2}): `{3}'",
            ClientId,
            e.ApplicationMessage.Topic,
            e.ApplicationMessage.Payload?.Length ?? 0,
            e.ApplicationMessage.ConvertPayloadToString());

        var _ = TryProcessDeviceStatus(e.ApplicationMessage)
                || TryProcessApiInfo(e.ApplicationMessage)
                || TryProcessApiMessage(e.ApplicationMessage)
                || TryProcessResponse(e.ApplicationMessage);

        return Task.CompletedTask;
    }

    bool TryProcessDeviceStatus(MqttApplicationMessage message)
    {
        var success = false;

        if (message.Payload is { })
        {
            var m = DeviceStatusRegex.Match(message.Topic);

            if (m.Success)
            {
                try
                {
                    DeviceStatusReceived?.Invoke(this, new DeviceStatusEventArgs(
                        new DeviceDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        Serializer.Deserialize<EDeviceStatus>(message.Payload)));
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

    bool TryProcessApiInfo(MqttApplicationMessage message)
    {
        var success = false;

        if (message.Payload is { })
        {
            var m = ApiVersionRegex.Match(message.Topic);

            if (m.Success)
            {
                try
                {
                    ApiInfoReceived?.Invoke(this, new ApiInfoEventArgs(
                        new DeviceDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        m.Groups[4].Value,
                        message.ConvertPayloadToString()));
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

    bool TryProcessApiMessage(MqttApplicationMessage message)
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

            try
            {
                ApiMessageReceived?.Invoke(this, new ApiMessageEventArgs(
                    new DeviceDetails(
                        m.Groups[1].Value,
                        m.Groups[2].Value,
                        Convert.ToInt32(m.Groups[3].Value)),
                    m.Groups[4].Value,
                    m.Groups[6].Value,
                    message.Payload,
                    (m.Groups[5].Value == ">")
                        ? EDirection.Out
                        : EDirection.In,
                    responseTopic,
                    messageId));
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }

        return m.Success;
    }

    public Task SubscribeToDeviceStatusAsync(DeviceFilter filter, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = filter;

        return _client.SubscribeAsync(
            $"{domain}/{kind}/{id}/status",
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    bool TryProcessResponse(MqttApplicationMessage message)
    {
        var success = false;

        if (message.Payload is { }
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

                try
                {
                    ResponseReceived?.Invoke(this, new ResponseEventArgs(
                        new DeviceDetails(
                            m.Groups[1].Value,
                            m.Groups[2].Value,
                            Convert.ToInt32(m.Groups[3].Value)),
                        messageId,
                        message.Payload));
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

    public Task SendDeviceStatusAsync(DeviceStatusMessage message, CancellationToken cancellationToken)
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

    public Task SubscribeToApiInfoAsync(ApiFilter filter, CancellationToken cancellationToken)
    {
        var (domain, kind, id, api) = filter;

        return _client.SubscribeAsync(
            $"{domain}/{kind}/{id}/a/{api}/version",
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    public async Task SendApiInfoAsync(ApiInfoMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), topic, version) = message;

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/a/{topic}/version")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag();

        await _client.PublishAsync(
            builder
                .WithPayload(version)
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

    public Task SubscribeToApiMessagesAsync(DeviceDetails device, string api, EDirection direction,
        CancellationToken cancellationToken)
    {
        var (domain, kind, id) = device;

        var topic = $"{domain}/{kind}/{id}/a/{api}/{((direction == EDirection.In) ? "<" : ">")}/+";

        return _client.SubscribeAsync(
            topic,
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
    }

    public Task SendApiMessageAsync(ApiMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), api, topic, options, direction, payload, _, _) = message;

        var json = Array.Empty<byte>();

        if (payload is { })
        {
            json = Serializer.Serialize(payload);
        }

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/a/{api}/{((direction == EDirection.In) ? "<" : ">")}/{topic}")
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(options.Retain)
            .WithMessageExpiryInterval(options.TimeToLive);

        if (message.ResponseTopic is { })
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

    public Task SubscribeResponseAsync(DeviceDetails device, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = device;

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

    public void Dispose()
    {
        try
        {
            DeleteRetainedMessagesAsync().Wait();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }

        _client.Dispose();

        CancelSendTask();
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