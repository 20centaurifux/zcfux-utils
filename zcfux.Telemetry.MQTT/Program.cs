using zcfux.Telemetry.Discovery;

namespace zcfux.Telemetry.MQTT
{
    public class Program
    {
        static class Domains
        {
            public static readonly string Device = "d";
        }

        static class Kinds
        {
            public static readonly string Bulb = "bulb";
        }

        [Api(Topic = "pwr", Version = "1.0")]
        public interface IPowerApi
        {
            [Event(Topic = "status", Retain = true)]
            IAsyncEnumerable<bool> On { get; }

            [Command(Topic = "status", TimeToLive = 5)]
            Task ChangeStatusAsync(bool on);

            [Command(Topic = "toggle", TimeToLive = 5)]
            Task<bool> ToggleAsync();
        }

        sealed class PowerImpl : IPowerApi, IConnected
        {
            readonly object _lock = new();
            bool _state;

            readonly Producer<bool> _producer = new();

            public IAsyncEnumerable<bool> On => _producer;

            public Task ChangeStatusAsync(bool on)
            {
                lock (_lock)
                {
                    _state = on;
                }

                _producer.Publish(on);

                return Task.CompletedTask;
            }

            public Task<bool> ToggleAsync()
            {
                var newState = false;

                lock (_lock)
                {
                    newState = !_state;

                    _state = newState;
                }

                _producer.Publish(newState);

                return Task.FromResult(newState);
            }

            public void Connected()
            {
                _producer.Publish(_state);
            }
        }

        public sealed class Bulb : Device.Client
        {
            public IPowerApi Power { get; } = new PowerImpl();

            public Bulb(Device.Options options)
                : base(options)
            {
            }
        }

        static async Task RunClientAsync(CancellationToken cancellationToken)
        {
            var connectionOptions = new ConnectionOptionsBuilder()
                .WithClientOptions(new ClientOptionsBuilder()
                    .WithClientId("1")
                    .WithLastWill(new LastWillOptionsBuilder()
                        .WithDomain(Domains.Device)
                        .WithKind(Kinds.Bulb)
                        .WithId(23)
                        .WithMessageOptions(new MessageOptions(Retain: true))
                        .Build())
                    .Build())
                .WithMessageQueue(new MemoryMessageQueue(50))
                .WithReconnect(TimeSpan.FromSeconds(30))
                .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .WithCleanupRetainedMessages()
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                var options = new Device.OptionsBuilder()
                    .WithDomain(Domains.Device)
                    .WithKind(Kinds.Bulb)
                    .WithId(23)
                    .WithSerializer(new Serializer())
                    .WithConnection(connection)
                    .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                using (var bulb = new Bulb(options))
                {
                    await connection.ConnectAsync(CancellationToken.None);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        }
                        catch { }
                    }
                }

                await connection.DisconnectAsync(CancellationToken.None);
            }
        }

        static async Task RunControllerAsync(CancellationToken cancellationToken)
        {
            var connectionOptions = new ConnectionOptionsBuilder()
                .WithClientOptions(new ClientOptionsBuilder()
                    .WithClientId("2")
                    .Build())
                .WithMessageQueue(new MemoryMessageQueue(50))
                .WithReconnect(TimeSpan.FromSeconds(30))
                .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                var options = new Device.OptionsBuilder()
                    .WithDomain(Domains.Device)
                    .WithKind(Kinds.Bulb)
                    .WithId(23)
                    .WithSerializer(new Serializer())
                    .WithConnection(connection)
                    .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                var proxy = Device.ProxyFactory.CreateApiProxy<IPowerApi>(options);

                var reader = proxy.On.GetAsyncEnumerator(cancellationToken);

                await connection.ConnectAsync(CancellationToken.None);

                var task = reader.MoveNextAsync();

                await proxy.ChangeStatusAsync(true);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (task.IsCompleted)
                        {
                            var value = reader.Current;

                            task = reader.MoveNextAsync();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                        await proxy.ToggleAsync();
                    }
                    catch (Exception ex) { }
                }

                await connection.DisconnectAsync(CancellationToken.None);
            }
        }

        static async Task RunDiscovererAsync(CancellationToken cancellationToken)
        {
            var connectionOptions = new ConnectionOptionsBuilder()
                .WithClientOptions(new ClientOptionsBuilder()
                    .WithClientId("discoverer")
                    .Build())
                .WithMessageQueue(new MemoryMessageQueue(50))
                .WithReconnect(TimeSpan.FromSeconds(30))
                .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                var apiRegistry = new Discovery.ApiRegistry();

                apiRegistry.Register<IPowerApi>();

                var options = new Discovery.OptionsBuilder()
                    .WithConnection(connection)
                    .WithFilter(new DeviceFilter("d", DeviceFilter.All, DeviceFilter.All))
                    .WithApiRegistry(apiRegistry)
                    .WithSerializer(new Serializer())
                    .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                var discoverer = new Discovery.Discoverer(options);

                IDiscoveredDevice? discoveredDevice = null;

                discoverer.Discovered += (s1, e1) =>
                {
                    discoveredDevice = e1.Device;

                    discoveredDevice.StatusChanged += (s2, e2) =>
                    {
                        var status = e2.Status;
                    };

                    discoveredDevice.Registered += (s2, e2) =>
                    {
                        var api = e2.Api;
                    };
                };

                await connection.ConnectAsync(CancellationToken.None);

                var status = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                        if (discoveredDevice?.TryGetApi<IPowerApi>() is { } powerApi)
                        {
                            await powerApi.ChangeStatusAsync(on: status);

                            status = !status;
                        }
                    }
                    catch { }
                }

                await connection.DisconnectAsync(CancellationToken.None);
            }
        }

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var tasks = new Task[]
            {
                RunClientAsync(cancellationTokenSource.Token),
                //RunDiscovererAsync(cancellationTokenSource.Token),
                RunControllerAsync(cancellationTokenSource.Token)
            };

            Console.ReadLine();

            cancellationTokenSource.Cancel();

            Task.WhenAll(tasks);
        }
    }
}