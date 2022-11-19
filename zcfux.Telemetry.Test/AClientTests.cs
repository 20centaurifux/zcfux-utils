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

namespace zcfux.Telemetry.Test;

public abstract class AClientTests
{
    sealed class SimpleClient : Device.Client, IConnected, IDisconnected
    {
        const long Offline = 0;
        const long Online = 1;

        long _state = Offline;

        public bool IsOnline
            => Interlocked.Read(ref _state) == Online;

        public SimpleClient(Device.Options options) : base(options)
        {
        }

        public void Connected()
            => Interlocked.Exchange(ref _state, Online);

        public void Disconnected()
            => Interlocked.Exchange(ref _state, Offline);
    }

    [Api(Topic = "useless", Version = "1.0")]
    interface IOnlineApi
    {
        [Event(Topic = "online")]
        IAsyncEnumerable<bool> Online { get; }
    }

    sealed class OnlineImpl : IOnlineApi, IConnected, IDisconnected
    {
        readonly Producer<bool> _producer = new();

        public IAsyncEnumerable<bool> Online => _producer;

        public void Connected()
            => _producer.Write(true);

        public void Disconnected()
            => _producer.Write(false);
    }

    sealed class OnlineClient : Device.Client
    {
        public IOnlineApi Api { get; } = new OnlineImpl();

        public OnlineClient(Device.Options options) : base(options)
        {
        }
    }

    [Api(Topic = "power", Version = "1.0")]
    interface IPowerApi
    {
        [Event(Topic = "on")]
        IAsyncEnumerable<bool> On { get; }

        [Command(Topic = "on")]
        Task SetStateAsync(bool on);

        [Command(Topic = "toggle", ResponseTimeout = 1)]
        Task<bool> ToggleAsync();
    }

    sealed class PowerImpl : IPowerApi
    {
        readonly object _lock = new();
        bool _value;

        readonly Producer<bool> _producer = new();

        public IAsyncEnumerable<bool> On => _producer;

        public Task SetStateAsync(bool on)
        {
            lock (_lock)
            {
                _value = on;
            }

            _producer.Write(on);

            return Task.CompletedTask;
        }

        public Task<bool> ToggleAsync()
        {
            bool newState;

            lock (_lock)
            {
                newState = !_value;
                _value = newState;
            }

            _producer.Write(newState);

            return Task.FromResult(newState);
        }
    }

    sealed class Bulb : Device.Client
    {
        public IPowerApi Power { get; } = new PowerImpl();

        public Bulb(Device.Options options) : base(options)
        {
        }
    }

    [Test]
    public async Task ClientIsConnecting()
    {
        var device = new DeviceDetails("d", "simple", 1);

        using (var connection = CreateDeviceConnection(device))
        {
            var opts = CreateDeviceOptions(device, connection);

            using (var client = new SimpleClient(opts))
            {
                Assert.IsFalse(client.IsOnline);

                await connection.ConnectAsync();

                Assert.IsTrue(client.IsOnline);
            }
        }
    }

    [Test]
    public async Task ClientIsDisconnecting()
    {
        var device = new DeviceDetails("d", "simple", 1);

        using (var connection = CreateDeviceConnection(device))
        {
            var opts = CreateDeviceOptions(device, connection);

            using (var client = new SimpleClient(opts))
            {
                Assert.IsFalse(client.IsOnline);

                await connection.ConnectAsync();

                Assert.IsTrue(client.IsOnline);

                await connection.DisconnectAsync();

                Assert.IsFalse(client.IsOnline);
            }
        }
    }

    [Test]
    public async Task ApiIsConnecting()
    {
        var device = new DeviceDetails("d", "online", 1);

        using (var connection = CreateDeviceConnection(device))
        {
            var opts = CreateDeviceOptions(device, connection);

            using (var client = new OnlineClient(opts))
            {
                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection.ConnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsTrue(reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task ApiIsDisconnecting()
    {
        var device = new DeviceDetails("d", "online", 1);

        using (var connection = CreateDeviceConnection(device))
        {
            var opts = CreateDeviceOptions(device, connection);

            using (var client = new OnlineClient(opts))
            {
                await using (var reader = client.Api.Online.GetAsyncEnumerator())
                {
                    await connection.ConnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsTrue(reader.Current);

                    await connection.DisconnectAsync();

                    await reader.MoveNextAsync();

                    Assert.IsFalse(reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task SubscribeToEvents()
    {
        var device = new DeviceDetails("d", "bulb", 1);

        using (var connection = CreateDeviceConnection(device))
        {
            var opts = CreateDeviceOptions(device, connection);

            using (var sender = new Bulb(opts))
            {
                await connection.ConnectAsync();

                await using (var reader = sender.Power.On.GetAsyncEnumerator())
                {
                    for (var i = 0; i < 10; ++i)
                    {
                        var set = await sender.Power.ToggleAsync();

                        await reader.MoveNextAsync();

                        Assert.AreEqual(set, reader.Current);
                    }
                }
            }
        }
    }

    Device.Options CreateDeviceOptions(DeviceDetails device, IConnection connection)
    {
        var opts = new Device.OptionsBuilder()
            .WithConnection(connection)
            .WithDevice(device)
            .WithSerializer(CreateSerializer())
            .Build();

        return opts;
    }

    protected abstract IConnection CreateDeviceConnection(DeviceDetails device);

    protected abstract ISerializer CreateSerializer();
}