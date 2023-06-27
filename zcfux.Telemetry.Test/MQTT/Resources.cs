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
using zcfux.Logging;
using zcfux.Telemetry.Discovery;
using zcfux.Telemetry.MQTT;

namespace zcfux.Telemetry.Test.MQTT;

sealed class Resources : IDisposable
{
    static readonly MqttFactory MqttFactory = new();

    MqttServer? _server;

    Resources()
    {
    }

    public static Resources Setup()
    {
        var resources = new Resources();

        resources._server = CreateServer();

        resources._server.StartAsync().Wait();

        return resources;
    }
    
    static MqttServer CreateServer()
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
    
    static int GetPort()
    {
        var port = Environment.GetEnvironmentVariable("MQTT_TEST_PORT")
                   ?? "8883";

        return int.Parse(port);
    }

    public void Dispose()
    { 
        _server?.StopAsync().Wait();
        _server?.Dispose();
    }
    
    public static IConnection CreateConnection()
    {
        var port = GetPort();

        var factory = new Logging.Factory("connection");

        var logger = factory.FromName("console");

        logger.Verbosity = ESeverity.Trace;

        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithClientId(TestContext.CurrentContext.Random.GetString())
                .WithPort(port)
                .WithSessionTimeout(30)
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
            .WithLogger(logger)
            .Build();

        return new Connection(opts);
    }

    public static IConnection CreateNodeConnection(NodeDetails nodeDetails)
    {
        var port = GetPort();

        var factory = new Logging.Factory("node");
        
        var logger = factory.FromName("console");

        logger.Verbosity = ESeverity.Trace;
        
        var opts = new ConnectionOptionsBuilder()
            .WithClientOptions(new ClientOptionsBuilder()
                .WithClientId(TestContext.CurrentContext.Random.GetString())
                .WithPort(port)
                .WithSessionTimeout(30)
                .WithLastWill(new LastWillOptionsBuilder()
                    .WithDomain(nodeDetails.Domain)
                    .WithKind(nodeDetails.Kind)
                    .WithId(nodeDetails.Id)
                    .WithMessageOptions(new MessageOptions(Retain: true))
                    .Build())
                .Build())
            .WithMessageQueue(new MemoryMessageQueue(50))
            .WithCleanupRetainedMessages()
            .WithLogger(logger)
            .Build();

        return new Connection(opts);
    }

    public static Telemetry.Node.Options CreateNodeOptions(NodeDetails node, IConnection connection, double eventSubscriberTimeoutSeconds = .5)
    {
        var factory = new Logging.Factory("node");

        var logger = factory.FromName("console");

        logger.Verbosity = ESeverity.Trace;

        var opts = new Telemetry.Node.OptionsBuilder()
            .WithConnection(connection)
            .WithNode(node)
            .WithSerializer(CreateSerializer())
            .WithLogger(logger)
            .WithEventSubscriberTimeout(TimeSpan.FromSeconds(eventSubscriberTimeoutSeconds))
            .Build();

        return opts;
    }

    public static Discoverer CreateDiscoverer(IConnection connection, ApiRegistry apiRegistry)
    {
        var factory = new Logging.Factory("discoverer");

        var logger = factory.FromName("console");

        logger.Verbosity = Logging.ESeverity.Trace;

        var opts = new OptionsBuilder()
            .WithConnection(connection)
            .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
            .WithSerializer(CreateSerializer())
            .WithApiRegistry(apiRegistry)
            .WithLogger(logger)
            .Build();

        return new Discoverer(opts);
    }

    public static ISerializer CreateSerializer()
        => new Serializer();
}