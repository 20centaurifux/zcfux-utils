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
using MQTTnet.Server;
using zcfux.Telemetry.MQTT;

namespace zcfux.Telemetry.Test.MQTT;

public static class Factory
{
    public static MqttServer CreateServer()
    {
        var port = GetPort();

        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build();

        return new MqttFactory().CreateMqttServer(options);
    }

    public static IConnection CreateDeviceConnection(string domain, string kind, int id)
    {
        var port = GetPort();

        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithAddress("127.0.0.1")
                .WithPort(port)
                .WithSessionTimeout(5)
                .WithLastWill(new LastWillOptionsBuilder()
                    .WithDomain(domain)
                    .WithKind(kind)
                    .WithId(id)
                    .WithMessageOptions(new MessageOptions(Retain: true))
                    .Build())
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
            .WithCleanupRetainedMessages()
            .Build();

        return new Connection(opts);
    }

    public static IConnection CreateProxyConnection()
    {
        var port = GetPort();

        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithAddress("127.0.0.1")
                .WithPort(port)
                .WithSessionTimeout(5)
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
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