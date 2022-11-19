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
using zcfux.Telemetry.Device;
using zcfux.Telemetry.Discovery;
using Options = zcfux.Telemetry.Device.Options;
using OptionsBuilder = zcfux.Telemetry.Discovery.OptionsBuilder;

namespace zcfux.Telemetry.Test;

public abstract class ADiscoveryTests
{
    sealed class SimpleClient : Client
    {
        public SimpleClient(Options options) : base(options)
        {
        }
    }

    [Api(Topic = "Test", Version = "1.0")]
    public interface ITestApiV1_0
    {
        [Command(Topic = "add")]
        Task<int> AddAsync(Tuple<int, int> numbers);
    }

    sealed class TestApiV1_0_Impl : ITestApiV1_0
    {
        public Task<int> AddAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 + numbers.Item2);
    }

    sealed class ClientV1_0 : Client
    {
        public ITestApiV1_0 Math { get; } = new TestApiV1_0_Impl();

        public ClientV1_0(Options options) : base(options)
        {
        }
    }

    [Api(Topic = "Test", Version = "1.1")]
    public interface ITestApiV1_1 : ITestApiV1_0
    {
        [Command(Topic = "sub")]
        Task<int> SubAsync(Tuple<int, int> numbers);
    }

    sealed class TestApiV1_1_Impl : ITestApiV1_1
    {
        public Task<int> AddAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 + numbers.Item2);

        public Task<int> SubAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 - numbers.Item2);
    }

    sealed class ClientV1_1 : Client
    {
        public ITestApiV1_1 Math { get; } = new TestApiV1_1_Impl();

        public ClientV1_1(Options options) : base(options)
        {
        }
    }

    public enum EOperation
    {
        Add,
        Sub
    }

    public sealed record Request(EOperation Operation, int Op1, int Op2);

    [Api(Topic = "Test", Version = "2.0")]
    public interface ITestApiV2_0
    {
        [Command(Topic = "calc")]
        Task<int> CalcAsync(Request request);
    }

    sealed class TestApiV2_0_Impl : ITestApiV2_0
    {
        public Task<int> CalcAsync(Request request)
            => Task.FromResult(
                request.Operation switch
                {
                    EOperation.Add => request.Op1 + request.Op2,
                    EOperation.Sub => request.Op1 - request.Op2,
                    _ => throw new InvalidOperationException()
                });
    }

    sealed class ClientV2_0 : Client
    {
        public ITestApiV2_0 Math { get; } = new TestApiV2_0_Impl();

        public ClientV2_0(Options options) : base(options)
        {
        }
    }

    [Test]
    public async Task IsConnecting()
    {
        using (var connection = CreateConnection())
        {
            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(new ApiRegistry())
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            discoverer.Connected += (_, _) =>
            {
                taskCompletionSource.SetResult(true);
            };

            await connection.ConnectAsync();

            var connected = await taskCompletionSource.Task;

            Assert.IsTrue(connected);
        }
    }

    [Test]
    public async Task IsDisconnecting()
    {
        using (var connection = CreateConnection())
        {
            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(new ApiRegistry())
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            discoverer.Disconnected += (_, _) =>
            {
                taskCompletionSource.SetResult(true);
            };

            await connection.ConnectAsync();

            await connection.DisconnectAsync();

            var connected = await taskCompletionSource.Task;

            Assert.IsTrue(connected);
        }
    }

    [Test]
    public async Task DiscoversDevice()
    {
        using (var connection = CreateConnection())
        {
            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(new ApiRegistry())
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            discoverer.Discovered += (_, e) =>
            {
                taskCompletionSource.SetResult(e.Device);
            };

            await connection.ConnectAsync();

            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            using (var deviceConnection = CreateDeviceConnection(device))
            {
                var deviceOpts = new Device.OptionsBuilder()
                    .WithConnection(deviceConnection)
                    .WithDevice(device)
                    .WithSerializer(CreateSerializer())
                    .Build();

                using (new SimpleClient(deviceOpts))
                {
                    await deviceConnection.ConnectAsync();
                    await deviceConnection.DisconnectAsync();
                }
            }

            var discoveredDevice = await taskCompletionSource.Task;

            Assert.AreEqual(discoveredDevice.Domain, device.Domain);
            Assert.AreEqual(discoveredDevice.Kind, device.Kind);
            Assert.AreEqual(discoveredDevice.Id, device.Id);
        }
    }

    [Test]
    public async Task RegisterMatchingApiVersion()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_0>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.Registered += (_, _) =>
                {
                    taskCompletionSource.SetResult(e1.Device);
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientTask = StartClientAsync<ClientV1_0>(cancellationTokenSource.Token);

            var discoveredDevice = await taskCompletionSource.Task;

            Assert.IsTrue(discoveredDevice.HasApi<ITestApiV1_0>());

            var api = discoveredDevice.TryGetApi<ITestApiV1_0>();

            Assert.IsInstanceOf<ITestApiV1_0>(api);

            var sum = await api!.AddAsync(new Tuple<int, int>(23, 42));

            Assert.AreEqual(65, sum);

            cancellationTokenSource.Cancel();

            await clientTask;
        }
    }

    [Test]
    public async Task RegisterCompatibleApiVersion()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_0>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.Registered += (_, _) =>
                {
                    taskCompletionSource.SetResult(e1.Device);
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientTask = StartClientAsync<ClientV1_1>(cancellationTokenSource.Token);

            var discoveredDevice = await taskCompletionSource.Task;

            Assert.IsTrue(discoveredDevice.HasApi<ITestApiV1_0>());

            var api = discoveredDevice.TryGetApi<ITestApiV1_0>();

            Assert.IsInstanceOf<ITestApiV1_0>(api);

            var sum = await api!.AddAsync(new Tuple<int, int>(23, 42));

            Assert.AreEqual(65, sum);

            cancellationTokenSource.Cancel();

            await clientTask;
        }
    }

    [Test]
    public async Task DontRegisterIncompatibleApiVersion()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_1>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new DeviceFilter(DeviceFilter.All, DeviceFilter.All, DeviceFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.Registered += (_, _) =>
                {
                    taskCompletionSource.SetResult(e1.Device);
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientTask = StartClientAsync<ClientV2_0>(cancellationTokenSource.Token);

            var success = taskCompletionSource.Task.Wait(TimeSpan.FromSeconds(5));

            Assert.IsFalse(success);

            cancellationTokenSource.Cancel();

            await clientTask;
        }
    }

    Task StartClientAsync<T>(CancellationToken cancellationToken)
        where T : Client
    {
        return Task.Factory.StartNew(async () =>
        {
            var device = new DeviceDetails("d", "test", TestContext.CurrentContext.Random.Next());

            using (var deviceConnection = CreateDeviceConnection(device))
            {
                var deviceOpts = new Device.OptionsBuilder()
                    .WithConnection(deviceConnection)
                    .WithDevice(device)
                    .WithSerializer(CreateSerializer())
                    .Build();

                var client = (Activator.CreateInstance(typeof(T), deviceOpts) as T)!;

                using (client)
                {
                    await deviceConnection.ConnectAsync(cancellationToken);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(500, cancellationToken);
                    }

                    await deviceConnection.DisconnectAsync(cancellationToken);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    protected abstract IConnection CreateConnection();

    protected abstract IConnection CreateDeviceConnection(DeviceDetails device);

    protected abstract ISerializer CreateSerializer();
}