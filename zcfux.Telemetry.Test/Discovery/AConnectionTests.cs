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

namespace zcfux.Telemetry.Test.Discovery;

abstract class AConnectionTests : ATelemetryTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    [Repeat(100)]
    public async Task IsConnecting()
    {
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                var tcs = new TaskCompletionSource();

                discoverer.ConnectedAsync += _ =>
                {
                    tcs.SetResult();

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                await tcs.Task.WaitAsync(Timeout);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task IsDisconnecting()
    {
        await using (var connection = CreateConnection())
        {
            using (var discoverer = CreateDiscoverer(connection))
            {
                var tcs = new TaskCompletionSource();

                discoverer.DisconnectedAsync += _ =>
                {
                    tcs.SetResult();

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                await connection
                    .ConnectAsync()
                    .WaitAsync(Timeout);

                await connection
                    .DisconnectAsync()
                    .WaitAsync(Timeout);

                await tcs.Task.WaitAsync(Timeout);
            }
        }
    }

    [Test]
    [Repeat(100)]
    public async Task SetupTriggersConnectionHandler()
    {
        await using (var connection = CreateConnection())
        {
            await connection
                .ConnectAsync()
                .WaitAsync(Timeout);

            using (var discoverer = CreateDiscoverer(connection))
            {
                var tcs = new TaskCompletionSource();

                discoverer.ConnectedAsync += _ =>
                {
                    tcs.SetResult();

                    return Task.CompletedTask;
                };

                await discoverer.SetupAsync();

                await tcs.Task.WaitAsync(Timeout);
            }
        }
    }
}