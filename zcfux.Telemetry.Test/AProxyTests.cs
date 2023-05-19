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
using NUnit.Framework.Constraints;
using zcfux.Telemetry.Device;

namespace zcfux.Telemetry.Test;

public abstract class AProxyTests
{
    static readonly TimeSpan SendNumberDelay = TimeSpan.FromSeconds(1);
    
    [Api(Topic = "test", Version = "1.0")]
    public interface ITestApiV1
    {
        [Event(Topic = "number", TimeToLive = 5)]
        IAsyncEnumerable<long> Number { get; }

        [Command(Topic = "expire", ResponseTimeout = 1)]
        Task<bool> ExpireAsync();

        [Command(Topic = "number", TimeToLive = 1)]
        Task SendNumberAsync(long number);

        [Command(Topic = "double", TimeToLive = 1)]
        Task<long> DoubleAsync(long number);
    }

    sealed class TestV1Impl : ITestApiV1
    {
        readonly Producer<long> _producer = new();

        public IAsyncEnumerable<long> Number => _producer;

        public async Task<bool> ExpireAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            return true;
        }

        public async Task SendNumberAsync(long number)
        {
            await Task.Delay(SendNumberDelay);

            _producer.Write(number);
        }

        public Task<long> DoubleAsync(long number)
            => Task.FromResult(number * 2);
    }

    sealed class TestV1 : Client
    {
        public ITestApiV1 V1 { get; } = new TestV1Impl();

        public TestV1(Options options) : base(options)
        {
        }
    }

    [Api(Topic = "test", Version = "2.0")]
    public interface ITestApiV2
    {
        [Command(Topic = "identity", TimeToLive = 1)]
        Task<long> IdentityAsync(long number);
    }

    sealed class TestV2Impl : ITestApiV2
    {
        public Task<long> IdentityAsync(long number)
            => Task.FromResult(number);
    }

    sealed class TestV2 : Client
    {
        public ITestApiV2 V2 { get; } = new TestV2Impl();

        public TestV2(Options options) : base(options)
        {
        }
    }

    [Test]
    public async Task CommandIsCancelledWhenDisconnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            var task = await Task.WhenAny(proxy.SendNumberAsync(TestContext.CurrentContext.Random.Next()));

            Assert.IsTrue(task.IsCanceled);
        }
    }

    [Test]
    public async Task CommandIsCancelledWhenOnlyProxyIsConnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            await proxyConnection.ConnectAsync();

            var device = new DeviceDetails("d", "test", 1);

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            var task = await Task.WhenAny(proxy.SendNumberAsync(TestContext.CurrentContext.Random.Next()));

            Assert.IsTrue(task.IsCanceled);
        }
    }

    [Test]
    public async Task CommandIsCancelledWhenDeviceIsIncompatible()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV2(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                var task = await Task.WhenAny(proxy.SendNumberAsync(value));

                Assert.IsTrue(task.IsCanceled);
            }
        }
    }

    [Test]
    public async Task CommandIsEnqueued()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV1(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                var task = await Task.WhenAny(proxy.SendNumberAsync(value));

                Assert.IsTrue(task.IsCompletedSuccessfully);
            }
        }
    }

    [Test]
    public async Task ReceiveEvents()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV1(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
#pragma warning disable CS4014
                    proxy.SendNumberAsync(value);
#pragma warning restore CS4014

                    var success = await reader.MoveNextAsync();

                    Assert.IsTrue(success);
                    Assert.AreEqual(value, reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task ReceiveEventsAfterProxyReconnect()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV1(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                // trigger event & disconnect proxy
                await proxy.SendNumberAsync(value);
                await proxyConnection.DisconnectAsync();

                // reconnect proxy *after* device has sent event
                await Task.Delay(SendNumberDelay);
                await proxyConnection.ConnectAsync();

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
                    var moveNextTask = reader
                        .MoveNextAsync()
                        .AsTask();

                    if (await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(5))) == moveNextTask)
                    {
                        Assert.IsTrue(moveNextTask.Result);
                        Assert.AreEqual(value, reader.Current);
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }
            }
        }
    }

    [Test]
    public async Task EmptyEventsWhenDisconnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            await using (var reader = proxy.Number.GetAsyncEnumerator())
            {
                var success = await reader.MoveNextAsync();

                Assert.IsFalse(success);
            }
        }
    }

    [Test]
    public async Task EventsStopWhenConnectionIsClosed()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV1(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
                    await proxyConnection.DisconnectAsync();

                    var success = await reader.MoveNextAsync();

                    Assert.IsFalse(success);
                }
            }
        }
    }

    [Test]
    public async Task EmptyEventsWhenOnlyProxyIsConnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            await proxyConnection.ConnectAsync();

            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            await using (var reader = proxy.Number.GetAsyncEnumerator())
            {
                var success = await reader.MoveNextAsync();

                Assert.IsFalse(success);
            }
        }
    }

    [Test]
    public async Task ReceiveEventsAfterReconnect()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV1(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                await proxy.SendNumberAsync(value);

                await deviceConnection.DisconnectAsync();

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
#pragma warning disable CS4014
                    deviceConnection.ConnectAsync();
#pragma warning restore CS4014

                    var success = await reader.MoveNextAsync();

                    Assert.IsTrue(success);
                    Assert.AreEqual(value, reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task EmptyEventsWhenDeviceIsIncompatible()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOpts = CreateDeviceOptions(device, deviceConnection);

            using (new TestV2(deviceOpts))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
                    var success = await reader.MoveNextAsync();

                    Assert.IsFalse(success);
                }
            }
        }
    }

    [Test]
    public async Task SendCommandAndReceiveEvent()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOpts = CreateDeviceOptions(device, deviceConnection);

            using (new TestV1(deviceOpts))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                await using (var reader = proxy.Number.GetAsyncEnumerator())
                {
                    var number = TestContext.CurrentContext.Random.Next();

                    await proxy.SendNumberAsync(number);

                    var success = await reader.MoveNextAsync();

                    Assert.IsTrue(success);
                    Assert.AreEqual(number, reader.Current);
                }
            }
        }
    }

    [Test]
    public async Task RequestIsCancelledWhenDisconnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            var task = await Task.WhenAny(proxy.DoubleAsync(TestContext.CurrentContext.Random.Next()));

            Assert.IsTrue(task.IsCanceled);
        }
    }

    [Test]
    public async Task RequestIsCancelledWhenOnlyProxyIsConnected()
    {
        using (var proxyConnection = CreateProxyConnection())
        {
            await proxyConnection.ConnectAsync();

            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            var proxyOpts = CreateDeviceOptions(device, proxyConnection);

            var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

            var task = await Task.WhenAny(proxy.DoubleAsync(TestContext.CurrentContext.Random.Next()));

            Assert.IsTrue(task.IsCanceled);
        }
    }

    [Test]
    public async Task RequestIsCancelledWhenDeviceIsIncompatible()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOptions = CreateDeviceOptions(device, deviceConnection);

            using (var _ = new TestV2(deviceOptions))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var value = TestContext.CurrentContext.Random.Next();

                var task = await Task.WhenAny(proxy.DoubleAsync(value));

                Assert.IsTrue(task.IsCanceled);
            }
        }
    }

    [Test]
    public async Task RequestResponse()
    {
        var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOpts = CreateDeviceOptions(device, deviceConnection);

            using (new TestV1(deviceOpts))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                long n1 = TestContext.CurrentContext.Random.Next();

                var n2 = await proxy.DoubleAsync(n1);

                Assert.AreEqual(n1 * 2, n2);
            }
        }
    }

    [Test]
    public async Task ResponseExpires()
    {
        var device = new DeviceDetails("d", "test", 1);

        using (IConnection deviceConnection = CreateDeviceConnection(device),
               proxyConnection = CreateProxyConnection())
        {
            await Task.WhenAll(
                deviceConnection.ConnectAsync(),
                proxyConnection.ConnectAsync());

            var deviceOpts = CreateDeviceOptions(device, deviceConnection);

            using (new TestV1(deviceOpts))
            {
                var proxyOpts = CreateDeviceOptions(device, proxyConnection);

                var proxy = ProxyFactory.CreateApiProxy<ITestApiV1>(proxyOpts);

                var task = proxy.ExpireAsync();

                await Task.WhenAny(task);

                Assert.IsTrue(task.IsCanceled);
            }
        }
    }

    Options CreateDeviceOptions(DeviceDetails device, IConnection connection)
    {
        var opts = new OptionsBuilder()
            .WithConnection(connection)
            .WithDevice(device)
            .WithSerializer(CreateSerializer())
            .Build();

        return opts;
    }

    protected abstract IConnection CreateDeviceConnection(DeviceDetails device);

    protected abstract IConnection CreateProxyConnection();

    protected abstract ISerializer CreateSerializer();
}