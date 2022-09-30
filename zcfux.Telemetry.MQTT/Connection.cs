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
using Humanizer;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using zcfux.Logging;

namespace zcfux.Telemetry.MQTT;

public class Connection : IConnection
{
    static readonly MqttFactory Factory = new();

    static readonly Regex DeviceStatusRegex = new(
      "^([a-z0-9]+)/([a-z0-9]+)/([0-9]+)/status",
      RegexOptions.IgnoreCase);

    static readonly Regex ApiVersionRegex = new(
        "^([a-z0-9]+)/([a-z0-9]+)/([0-9]+)/a/([a-z0-9]+)/version",
        RegexOptions.IgnoreCase);

    static readonly Regex ApiTopicRegex = new(
        "^([a-z0-9]+)/([a-z0-9]+)/([0-9]+)/a/([a-z0-9]+)/([<|>])/([a-z0-9]+)",
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
    readonly AutoResetEvent _connectionEvent = new(false);

    readonly object _sendTaskLock = new();
    Task? _sendTask;

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

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Connecting client `{0}'.", ClientId);

        return _client.ConnectAsync(_clientOptions, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger?.Debug("Disconnecting client `{0}'.", ClientId);

        CancelReconnect();

        await DeleteRetainedMessagesAsync();

        Interlocked.Exchange(ref _connectivity, GracefulDisconnect);

        var opts = Factory.CreateClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectReason.DisconnectWithWillMessage)
            .Build();

        await _client.DisconnectAsync(opts, cancellationToken);
    }

    public async Task DeleteRetainedMessagesAsync()
    {
        if (IsConnected)
        {
            var tasks = _retainedMessages
                .Select(msg => _client.PublishAsync(msg, CancellationToken.None))
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }

    Task ClientConnected(MqttClientConnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' connected successfully.", ClientId);

        Interlocked.Exchange(ref _connectivity, Online);

        _connectionEvent.Set();

        Connected?.Invoke(this, EventArgs.Empty);

        StartSendTask();

        return Task.CompletedTask;
    }

    void StartSendTask()
    {
        lock (_sendTaskLock)
        {
            if (_sendTask == null)
            {
                _sendTask = Task.Run(async () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger?.Debug("Client `{0}' is waiting for messages to send.", ClientId);

                        if (_client.IsConnected
                            || _connectionEvent.WaitOne(TimeSpan.FromSeconds(1)))
                        {
                            var message = await _messageQueue.TryPeekAsync(_cancellationTokenSource.Token);

                            if (message is { })
                            {
                                _logger?.Debug(
                                    "Client `{0}' sends message to topic `{1}' (size={2}).",
                                    ClientId,
                                    message.Topic,
                                    message.Payload.Length);

                                _logger?.Trace(
                                    "Sending message (client=`{0}', topic=`{1}'): ? `{2}'",
                                    ClientId,
                                    message.Topic,
                                    message.ConvertPayloadToString());

                                try
                                {
                                    await _client.PublishAsync(message);

                                    _messageQueue.Dequeue();
                                }
                                catch (Exception ex)
                                {
                                    _logger?.Error(ex);
                                }
                            }
                        }
                        else
                        {
                            _logger?.Debug("Client `{0}' can't send message, device is offline.", ClientId);
                        }
                    }
                });
            }
        }
    }

    Task ClientDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger?.Debug("Client `{0}' disconnected.", ClientId);

        var previousStatus = Interlocked.Exchange(ref _connectivity, Offline);

        Disconnected?.Invoke(this, EventArgs.Empty);

        InitializeReconnect(previousStatus);

        return Task.CompletedTask;
    }

    void InitializeReconnect(long previousStatus)
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

                        _logger?.Debug("Reconnecting client `{0}' in {1}.", ClientId, _reconnect.Value.Humanize());

                        _reconnectTask = Task.Run(async () =>
                        {
                            await Task.Delay(_reconnect.Value, token);

                            lock(_reconnectLock)
                            {
                                _reconnectCancellationTokenSource = null;
                                _reconnectTask = null;
                            }

                            return ConnectAsync(token);
                        });
                    }
                }
            }
            else
            {
                CancelReconnect();
            }
        }
    }

    void CancelReconnect()
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

            Task.WhenAll(task).Wait();
        }
    }

    Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        _logger?.Trace("Client `{0}' received message in `{1}' (size={2}): `{3}'",
            ClientId,
            e.ApplicationMessage.Topic,
            (e.ApplicationMessage.Payload == null)
                ? 0
                : e.ApplicationMessage.Payload.Length,
            e.ApplicationMessage.ConvertPayloadToString());

        if (e.ApplicationMessage.Payload != null
            && e.ApplicationMessage.Payload.Any())
        {
            var _ = TryProcessDeviceStatus(e.ApplicationMessage)
                || TryProcessApiInfo(e.ApplicationMessage)
                || TryProcessApiMessage(e.ApplicationMessage);
        }

        return Task.CompletedTask;
    }

    bool TryProcessDeviceStatus(MqttApplicationMessage message)
    {
        var m = DeviceStatusRegex.Match(message.Topic);

        if (m.Success)
        {
            DeviceStatusReceived?.Invoke(this, new DeviceStatusEventArgs(
                new DeviceDetails(
                    m.Groups[1].Value,
                    m.Groups[2].Value,
                    Convert.ToInt32(m.Groups[3].Value)),
                Serializer.Deserialize<EDeviceStatus>(message.Payload)));
        }

        return m.Success;
    }

    bool TryProcessApiInfo(MqttApplicationMessage message)
    {
        var m = ApiVersionRegex.Match(message.Topic);

        if (m.Success)
        {
            ApiInfoReceived?.Invoke(this, new ApiInfoEventArgs(
                new DeviceDetails(
                    m.Groups[1].Value,
                    m.Groups[2].Value,
                    Convert.ToInt32(m.Groups[3].Value)),
                m.Groups[4].Value,
                message.ConvertPayloadToString()));
        }

        return m.Success;
    }

    bool TryProcessApiMessage(MqttApplicationMessage message)
    {
        var m = ApiTopicRegex.Match(message.Topic);

        if (m.Success)
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
                    : EDirection.In));
        }

        return m.Success;
    }

    public Task SubscribeToApiInfoAsync(DeviceDetails device, string api, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = device;

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

    public Task SubscribeToDeviceStatusAsync(DeviceDetails device, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = device;

        return _client.SubscribeAsync(
            $"{domain}/{kind}/{id}/status",
            MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken);
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

    public Task SubscribeToApiMessagesAsync(DeviceDetails device, string api, EDirection direction, CancellationToken cancellationToken)
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
        var ((domain, kind, id), api, topic, options, payload) = message;

        var json = Serializer.Serialize(payload);

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{domain}/{kind}/{id}/a/{api}/>/{topic}")
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(options.Retain)
            .WithMessageExpiryInterval(options.TimeToLive)
            .Build();

        return _messageQueue.EnqueueAsync(mqttMessage, message.Options.TimeToLive);
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

            if (_sendTask != null)
            {

                Task.WhenAny(_sendTask).Wait();
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }
}