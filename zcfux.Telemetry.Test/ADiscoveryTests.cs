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

        [Event(Topic = "rnd")]
        IAsyncEnumerable<int> Random { get; }
    }

    sealed class TestApiV1_0_Impl : ITestApiV1_0
    {
        readonly Producer<int> _producer = new();

        public Task<int> AddAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 + numbers.Item2);

        [Event(Topic = "rnd")]
        public IAsyncEnumerable<int> Random => _producer;
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
        readonly Producer<int> _producer = new();

        public Task<int> AddAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 + numbers.Item2);

        public Task<int> SubAsync(Tuple<int, int> numbers)
            => Task.FromResult(numbers.Item1 - numbers.Item2);

        [Event(Topic = "rnd")]
        public IAsyncEnumerable<int> Random => _producer;
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
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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

            var device = new NodeDetails("d", "test", TestContext.CurrentContext.Random.Next());

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
    public async Task DeviceStatusesReceived()
    {
        using (var connection = CreateConnection())
        {
            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(new ApiRegistry())
                .Build();

            var discoverer = new Discoverer(opts);

            var connectingEvent = new ManualResetEventSlim();
            var onlineEvent = new ManualResetEventSlim();
            var offlineEvent = new ManualResetEventSlim();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.StatusChanged += (_, e2) =>
                {
                    switch (e2.Status)
                    {
                        case ENodeStatus.Connecting:
                            connectingEvent.Set();
                            break;

                        case ENodeStatus.Online:
                            onlineEvent.Set();
                            break;

                        case ENodeStatus.Offline:
                            offlineEvent.Set();
                            break;
                    }
                };
            };

            await connection.ConnectAsync();

            var device = new NodeDetails("d", "test", TestContext.CurrentContext.Random.Next());

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

                    Assert.IsTrue(connectingEvent.Wait(TimeSpan.FromSeconds(5))
                                  && onlineEvent.Wait(TimeSpan.FromSeconds(5)));

                    await deviceConnection.DisconnectAsync();

                    Assert.IsTrue(offlineEvent.Wait(TimeSpan.FromSeconds(5)));
                }
            }
        }
    }

    [Test]
    public async Task RegisterMatchingApiVersionAndCallMethod()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_0>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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
    public async Task RegisterCompatibleApiVersionAndCallMethod()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_0>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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
    public async Task DropReplacedApiVersion()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_0>();
            registry.Register<ITestApiV2_0>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var discoveredDeviceTcs = new TaskCompletionSource<IDiscoveredDevice>();
            var v1RegisteredTcs = new TaskCompletionSource();
            var v1DroppedTcs = new TaskCompletionSource();
            var v2RegisteredTcs = new TaskCompletionSource();

            discoverer.Discovered += (_, e1) =>
            {
                discoveredDeviceTcs.SetResult(e1.Device);

                e1.Device.Registered += (_, e2) =>
                {
                    switch (e2.Version)
                    {
                        case "1.0":
                            v1RegisteredTcs.SetResult();
                            break;

                        case "2.0":
                            v2RegisteredTcs.SetResult();
                            break;
                    }
                };

                e1.Device.Dropped += (_, e2) =>
                {
                    if (e2.Version.Equals("1.0"))
                    {
                        v1DroppedTcs.SetResult();
                    }
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientId = TestContext.CurrentContext.Random.Next();

            var clientV1Task = StartClientAsync<ClientV1_0>(clientId, cancellationTokenSource.Token);

            var discoveredDevice = await discoveredDeviceTcs.Task;

            await v1RegisteredTcs.Task;

            Assert.IsTrue(discoveredDevice.HasApi<ITestApiV1_0>());
            Assert.IsFalse(discoveredDevice.HasApi<ITestApiV2_0>());

            var clientV2Task = StartClientAsync<ClientV2_0>(clientId, cancellationTokenSource.Token);

            await v1DroppedTcs.Task;
            await v2RegisteredTcs.Task;

            Assert.IsTrue(discoveredDevice.HasApi<ITestApiV2_0>());
            Assert.IsFalse(discoveredDevice.HasApi<ITestApiV1_0>());

            cancellationTokenSource.Cancel();

            await Task.WhenAll(clientV1Task, clientV2Task);
        }
    }
    
    [Test]
    public async Task TryGetApiFromOfflineDevice()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_1>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            var offlineEvent = new ManualResetEventSlim();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.Registered += (_, _) =>
                {
                    taskCompletionSource.SetResult(e1.Device);
                };

                e1.Device.StatusChanged += (_, e2) =>
                {
                    if (e2.Status == ENodeStatus.Offline)
                    {
                        offlineEvent.Set();
                    }
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientTask = StartClientAsync<ClientV1_1>(cancellationTokenSource.Token);

            var discoveredDevice = await taskCompletionSource.Task;

            var api = discoveredDevice.TryGetApi<ITestApiV1_1>();

            Assert.IsInstanceOf<ITestApiV1_1>(api);

            cancellationTokenSource.Cancel();

            await clientTask;

            Assert.IsTrue(offlineEvent.Wait(TimeSpan.FromSeconds(5)));

            api = discoveredDevice.TryGetApi<ITestApiV1_1>();

            Assert.IsInstanceOf<ITestApiV1_1>(api);
        }
    }

    [Test]
    public async Task SendCommandToOfflineDevice()
    {
        using (var connection = CreateConnection())
        {
            var registry = new ApiRegistry();

            registry.Register<ITestApiV1_1>();

            var opts = new OptionsBuilder()
                .WithConnection(connection)
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
                .WithSerializer(CreateSerializer())
                .WithApiRegistry(registry)
                .Build();

            var discoverer = new Discoverer(opts);

            var taskCompletionSource = new TaskCompletionSource<IDiscoveredDevice>();

            var offlineEvent = new ManualResetEventSlim();

            discoverer.Discovered += (_, e1) =>
            {
                e1.Device.Registered += (_, _) =>
                {
                    taskCompletionSource.SetResult(e1.Device);
                };

                e1.Device.StatusChanged += (_, e2) =>
                {
                    if (e2.Status == ENodeStatus.Offline)
                    {
                        offlineEvent.Set();
                    }
                };
            };

            await connection.ConnectAsync();

            var cancellationTokenSource = new CancellationTokenSource();

            var clientTask = StartClientAsync<ClientV1_1>(cancellationTokenSource.Token);

            var discoveredDevice = await taskCompletionSource.Task;

            var api = discoveredDevice.TryGetApi<ITestApiV1_1>();

            Assert.IsInstanceOf<ITestApiV1_1>(api);

            cancellationTokenSource.Cancel();

            await clientTask;

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await api!.AddAsync(new Tuple<int, int>(1, 1));
            });
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
                .WithFilter(new NodeFilter(NodeFilter.All, NodeFilter.All, NodeFilter.All))
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
        => StartClientAsync<T>(TestContext.CurrentContext.Random.Next(), cancellationToken);

    Task StartClientAsync<T>(int clientId, CancellationToken cancellationToken)
        where T : Client
    {
        return Task.Factory.StartNew(async () =>
        {
            var device = new NodeDetails("d", "test", clientId);

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
                    await deviceConnection.ConnectAsync(CancellationToken.None);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(500, CancellationToken.None);
                    }

                    await deviceConnection.DisconnectAsync(CancellationToken.None);
                }
            }
        }, TaskCreationOptions.LongRunning).Unwrap();
    }

    protected abstract IConnection CreateConnection();

    protected abstract IConnection CreateDeviceConnection(NodeDetails node);

    protected abstract ISerializer CreateSerializer();
}