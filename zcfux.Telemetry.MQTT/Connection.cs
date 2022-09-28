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
using System.Text.RegularExpressions;
using zcfux.Logging;

namespace zcfux.Telemetry.MQTT;

public class Connection : IConnection
{
    static readonly MqttFactory Factory = new();

    static readonly Regex ApiTopicRegex = new Regex(
        "^[a-z0-9]+/[a-z0-9]+/[0-9]+/a/([a-z0-9]+)/</([a-z0-9]+)",
        RegexOptions.IgnoreCase);

    static readonly ISerializer Serializer = new Serializer();

    const long Offline = 0;
    const long Online = 1;

    readonly ILogger? _logger;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly MqttClientOptions _clientOptions;
    readonly IMqttClient _client;

    readonly IMessageQueue _messageQueue;

    readonly bool _cleanupRetainedMessages;

    readonly object sendTaskLock = new();
    Task? _sendTask;

    long _connected = Offline;
    readonly AutoResetEvent _connectionEvent = new(false);

    readonly ConcurrentBag<MqttApplicationMessage> _retainedMessages = new();

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<ApiMessageEventArgs>? ApiMessageReceived;
    public event EventHandler<DeviceStatusEventArgs>? DeviceStatusReceived;

    public bool IsConnected => Interlocked.Read(ref _connected) == Online;

    public string ClientId => _clientOptions.ClientId;

    public Connection(ConnectionOptions options)
    {
        _logger = options.Logger;
        _messageQueue = options.MessageQueue;
        _cleanupRetainedMessages = options.CleanupRetainedMessages;

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
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithSessionExpiryInterval(0)
            .WithCleanSession();

        if (options.Tls)
        {
            builder = builder.WithTls(o =>
            {
                o.AllowUntrustedCertificates = options.AllowUntrustedCertificates;
            });
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

    public Task ConnectAsync()
        => _client.ConnectAsync(_clientOptions);

    public async Task DisconnectAsync()
    {
        await DeleteRetainedMessagesAsync();

        var opts = Factory.CreateClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectReason.DisconnectWithWillMessage)
            .Build();

        await _client.DisconnectAsync(opts);
    }

    public async Task DeleteRetainedMessagesAsync()
    {
        var tasks = _retainedMessages
            .Select(msg => _client.PublishAsync(msg, _cancellationTokenSource.Token))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    Task ClientConnected(MqttClientConnectedEventArgs e)
    {
        Interlocked.Exchange(ref _connected, Online);

        _connectionEvent.Set();

        Connected?.Invoke(this, EventArgs.Empty);

        StartSendTask();

        return Task.CompletedTask;
    }

    void StartSendTask()
    {
        lock (sendTaskLock)
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
                                    "Sending message (client=`{0}', topic=`{1}'): {2}",
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
        Interlocked.Exchange(ref _connected, Offline);

        Disconnected?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var m = ApiTopicRegex.Match(e.ApplicationMessage.Topic);

        if (m.Success
            && e.ApplicationMessage.Payload != null
            && e.ApplicationMessage.Payload.Any())
        {
            ApiMessageReceived?.Invoke(this, new ApiMessageEventArgs(
                m.Groups[1].Value,
                m.Groups[2].Value,
                e.ApplicationMessage.Payload));
        }

        return Task.CompletedTask;
    }
   
    public Task SendDeviceStatusAsync(DeviceStatusMessage message, CancellationToken cancellationToken)
    {
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{message.Device.Domain}/{message.Device.Kind}/{message.Device.Id}/status")
            .WithPayload(Convert.ToInt32(message.Status).ToString())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        return _client.PublishAsync(mqttMessage, cancellationToken);
    }

    public async Task SendApiInfoAsync(ApiInfoMessage message, CancellationToken cancellationToken)
    {
        var ((domain, kind, id), topic, version)= message;

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

    public Task SubscribeToApiMessageAsync(DeviceDetails device, EDirection direction, CancellationToken cancellationToken)
    {
        var (domain, kind, id) = device;

        var topic = $"{domain}/{kind}/{id}/a/+/{((direction == EDirection.In) ? "<" : ">")}/+";

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
            .WithMessageExpiryInterval(Convert.ToUInt32(options.TimeToLive.TotalSeconds))
            .Build();

        if (!_messageQueue.TryEnqueue(mqttMessage, options.TimeToLive))
        {
            _logger?.Warn("Couldn't enqueue api message (client=`{0}').", ClientId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            DeleteRetainedMessagesAsync().Wait();
        }
        catch(Exception ex)
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