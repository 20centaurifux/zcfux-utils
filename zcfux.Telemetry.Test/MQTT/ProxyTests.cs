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
using MQTTnet.Server;
using NUnit.Framework;
using zcfux.Telemetry.MQTT;

namespace zcfux.Telemetry.Test.MQTT;

public sealed class ProxyTests : AProxyTests
{
    MqttServer _server = default!;

    [SetUp]
    public void Setup()
    {
        _server = Factory.CreateServer();

        _server
            .StartAsync()
            .Wait();
    }

    [TearDown]
    public void Teardown()
        => _server
            .StopAsync()
            .Wait();

    protected override IConnection CreateDeviceConnection(DeviceDetails device)
        => Factory.CreateDeviceConnection(device.Domain, device.Kind, device.Id);

    protected override IConnection CreateProxyConnection()
        => Factory.CreateConnection();

    protected override ISerializer CreateSerializer()
        => new Serializer();
}