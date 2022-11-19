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
using zcfux.Logging;

namespace zcfux.Telemetry.MQTT;

public sealed class ConnectionOptionsBuilder
{
    ClientOptions? _clientOptions;
    IMessageQueue? _messageQueue;
    TimeSpan _retrySendingInterval = TimeSpan.FromSeconds(5);
    ILogger? _logger;
    bool _cleanupRetainedMessages;
    TimeSpan? _reconnect;

    ConnectionOptionsBuilder Clone()
    {
        var builder = new ConnectionOptionsBuilder
        {
            _clientOptions = _clientOptions,
            _messageQueue = _messageQueue,
            _retrySendingInterval = _retrySendingInterval,
            _logger = _logger,
            _cleanupRetainedMessages = _cleanupRetainedMessages,
            _reconnect = _reconnect
        };

        return builder;
    }

    public ConnectionOptionsBuilder WithClientOptions(ClientOptions clientOptions)
    {
        var builder = Clone();

        builder._clientOptions = clientOptions;

        return builder;
    }

    public ConnectionOptionsBuilder WithMessageQueue(IMessageQueue messageQueue)
    {
        var builder = Clone();

        builder._messageQueue = messageQueue;

        return builder;
    }

    public ConnectionOptionsBuilder WithRetrySendingInterval(TimeSpan interval)
    {
        var builder = Clone();

        builder._retrySendingInterval = interval;

        return builder;
    }

    public ConnectionOptionsBuilder WithLogger(ILogger? logger)
    {
        var builder = Clone();

        builder._logger = logger;

        return builder;
    }

    public ConnectionOptionsBuilder WithCleanupRetainedMessages(bool value = true)
    {
        var builder = Clone();

        builder._cleanupRetainedMessages = value;

        return builder;
    }

    public ConnectionOptionsBuilder WithReconnect(TimeSpan? reconnect)
    {
        var builder = Clone();

        builder._reconnect = reconnect;

        return builder;
    }

    public ConnectionOptions Build()
    {
        ThrowIfIncomplete();

        return new ConnectionOptions(
            _clientOptions!,
            _messageQueue!,
            _retrySendingInterval,
            _logger,
            _cleanupRetainedMessages,
            _reconnect);
    }

    void ThrowIfIncomplete()
    {
        if (_clientOptions == null)
        {
            throw new ArgumentException("ClientOptions cannot be null.");
        }

        if (_messageQueue == null)
        {
            throw new ArgumentException("MessageQueue cannot be null.");
        }
    }
}