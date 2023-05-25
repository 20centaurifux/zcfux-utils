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
using System.Security.Authentication;
using MQTTnet;
using MQTTnet.Server;
using NUnit.Framework;
using zcfux.Telemetry.MQTT;

namespace zcfux.Telemetry.Test.MQTT;

public static class Factory
{
    static readonly MqttFactory MqttFactory = new();
    
    public static MqttServer CreateServer()
    {
        var port = GetPort();

        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .WithEncryptionSslProtocol(SslProtocols.None)
            .WithPersistentSessions()
            .Build();

        return MqttFactory.CreateMqttServer(options);
    }

    public static IConnection CreateConnection()
    {
        var port = GetPort();

        var factory = new Logging.Factory("Connection");

        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithClientId(TestContext.CurrentContext.Random.GetString())
                .WithPort(port)
                .WithSessionTimeout(30)
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
            .WithLogger(factory.FromName("console"))
            .Build();

        return new Connection(opts);
    }

    public static IConnection CreateDeviceConnection(string domain, string kind, int id)
    {
        var port = GetPort();

        var factory = new Logging.Factory("device");
        
        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithClientId(TestContext.CurrentContext.Random.GetString())
                .WithPort(port)
                .WithSessionTimeout(30)
                .WithLastWill(new LastWillOptionsBuilder()
                    .WithDomain(domain)
                    .WithKind(kind)
                    .WithId(id)
                    .WithMessageOptions(new MessageOptions(Retain: true))
                    .Build())
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
            .WithCleanupRetainedMessages()
            .WithLogger(factory.FromName("console"))
            .Build();

        return new Connection(opts);
    }

    static int GetPort()
    {
        var port = Environment.GetEnvironmentVariable("MQTT_TEST_PORT")
                   ?? "8883";

        return int.Parse(port);
    }
}