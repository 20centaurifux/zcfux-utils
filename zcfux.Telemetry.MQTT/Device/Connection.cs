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
using System.Text.RegularExpressions;
using zcfux.Logging;
using zcfux.Telemetry.Device;

namespace zcfux.Telemetry.MQTT.Device;

public class Connection : IConnection
{
    static readonly MqttFactory Factory = new();

    static readonly Regex ApiTopicRegex = new Regex(
        "^[a-z0-9]+/[a-z0-9]+/[0-9]+/a/([a-z0-9]+)/i/([a-z0-9]+)",
        RegexOptions.IgnoreCase);

    static readonly ISerializer Serializer = new Serializer();

    const long Offline = 0;
    const long Online = 1;

    readonly ILogger? _logger;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly MqttClientOptions _clientOptions;
    readonly IMqttClient _client;

    readonly IMessageQueue _messageQueue;

    readonly object _taskLock = new();
    Task? _task;

    long _connected = Offline;
    readonly AutoResetEvent _connectionEvent = new AutoResetEvent(false);

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<ApiMessageEventArgs>? MessageReceived;

    public bool IsConnected => Interlocked.Read(ref _connected) == Online;

    public string Domain { get; }

    public string Kind { get; }

    public int Id { get; }

    public Connection(Options options)
    {
        (Domain, Kind, Id, _logger) = (options.Domain, options.Kind, options.Id, options.Logger);

        _clientOptions = BuildMqttClientOptions(options.ClientOptions);

        _client = Factory.CreateMqttClient();

        _client.ConnectedAsync += ClientConnected;
        _client.DisconnectedAsync += ClientDisconnected;
        _client.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;

        _messageQueue = options.MessageQueue;
    }

    MqttClientOptions BuildMqttClientOptions(ClientOptions options)
    {
        var builder = Factory.CreateClientOptionsBuilder()
            .WithTcpServer(options.Address, options.Port)
            .WithTimeout(options.Timeout)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithWillTopic($"{Domain}/{Kind}/{Id}/status")
            .WithWillPayload(Convert.ToInt32(EStatus.Offline).ToString())
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithWillRetain()
            .WithCleanSession();

        if (options.Tls)
        {
            builder = builder.WithTls(o =>
            {
                o.AllowUntrustedCertificates = options.AllowUntrustedCertificates;
            });
        }

        return builder.Build();
    }

    public Task ConnectAsync()
        => _client.ConnectAsync(_clientOptions);

    public Task DisconnectAsync()
    {
        var opts = Factory.CreateClientDisconnectOptionsBuilder()
            .WithReason(MqttClientDisconnectReason.DisconnectWithWillMessage)
            .Build();

        return _client.DisconnectAsync(opts);
    }

    async Task ClientConnected(MqttClientConnectedEventArgs e)
    {
        await _client.SubscribeAsync($"{Domain}/{Kind}/{Id}/a/+/i/+");

        Interlocked.Exchange(ref _connected, Online);

        _connectionEvent.Set();

        Connected?.Invoke(this, EventArgs.Empty);

        StartTask();
    }

    void StartTask()
    {
        lock (_taskLock)
        {
            if (_task == null)
            {
                _task = Task.Run(async () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger?.Debug(
                            "Device (domain=`{0}', kind=`{1}', id={2}) is waiting for messages to send.",
                            Domain,
                            Kind,
                            Id);

                        if (_client.IsConnected
                            || _connectionEvent.WaitOne(TimeSpan.FromSeconds(1)))
                        {
                            var message = await _messageQueue.TryPeekAsync(_cancellationTokenSource.Token);

                            if (message is { })
                            {
                                _logger?.Debug(
                                    "Device (domain=`{0}', kind=`{1}', id={2}) sends message to topic `{3}'.",
                                    Domain,
                                    Kind,
                                    Id,
                                    message.Topic);

                                _logger?.Trace("Topic `{0}' => `{1}'", message.Topic, message.ConvertPayloadToString());

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
                            _logger?.Debug(
                                "Cannot send message, device (domain=`{0}', kind=`{1}', id={2}) is offline.",
                                Domain,
                                Kind,
                                Id);
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

        if (m.Success)
        {
            MessageReceived?.Invoke(this, new ApiMessageEventArgs(
                m.Groups[1].Value,
                m.Groups[2].Value,
                e.ApplicationMessage.Payload));
        }

        return Task.CompletedTask;
    }

    public Task SendStatusAsync(EStatus status, CancellationToken cancellationToken)
    {
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{Domain}/{Kind}/{Id}/status")
            .WithPayload(Convert.ToInt32(status).ToString())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        if (!_messageQueue.TryEnqueue(mqttMessage, TimeSpan.Zero))
        {
            _logger?.Warn(
                "Couldn't enqeueue status message (domain=`{0}', kind=`{1}', id={2}, status=`{3}').",
                Domain,
                Kind,
                Id,
                status);
        }

        return Task.CompletedTask;
    }

    public Task SendApiMessageAsync(ApiMessage message, CancellationToken cancellationToken)
    {
        var (api, topic, options, payload) = message;

        var json = Serializer.Serialize(payload);

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"{Domain}/{Kind}/{Id}/a/{api}/o/{topic}")
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(options.Retain)
            .WithMessageExpiryInterval(Convert.ToUInt32(options.TimeToLive.TotalSeconds))
            .Build();

        if (!_messageQueue.TryEnqueue(mqttMessage, options.TimeToLive))
        {
            _logger?.Warn(
                "Couldn't enqeueue api message (domain=`{0}', kind=`{1}', id={2}, api=`{3}', topic=`{4}').",
                Domain,
                Kind,
                Id,
                message.Api,
                message.Topic);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Dispose();

        _cancellationTokenSource.Cancel();

        Task.WhenAny(_task).Wait();
    }
}