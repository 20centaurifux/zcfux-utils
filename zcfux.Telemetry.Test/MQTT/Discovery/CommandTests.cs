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
using NUnit.Framework;
using zcfux.Telemetry.Discovery;
using zcfux.Telemetry.Test.Discovery;

namespace zcfux.Telemetry.Test.MQTT.Discovery;

sealed class CommandTests : ACommandTests
{
    Resources? _resources;

    [SetUp]
    public void Setup()
        => _resources = Resources.Setup();

    [TearDown]
    public void Teardown()
        => _resources!.Dispose();

    protected override IConnection CreateConnection()
        => Resources.CreateConnection();

    protected override IConnection CreateNodeConnection(NodeDetails nodeDetails)
        => Resources.CreateNodeConnection(nodeDetails);

    protected override Telemetry.Node.Options CreateNodeOptions(NodeDetails node, IConnection connection)
        => Resources.CreateNodeOptions(node, connection);

    protected override Discoverer CreateDiscoverer(IConnection connection, ApiRegistry apiRegistry)
        => Resources.CreateDiscoverer(connection, apiRegistry);

    protected override ISerializer CreateSerializer()
        => Resources.CreateSerializer();
}