using zcfux.Telemetry.Device;

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
            Task ChangeStatus(bool on);
        }

        sealed class PowerImpl : IPowerApi, IConnected
        {
            bool _state;

            readonly Producer<bool> _producer = new();

            public IAsyncEnumerable<bool> On => _producer;

            public Task ChangeStatus(bool on)
            {
                _state = on;

                _producer.Publish(on);

                return Task.CompletedTask;
            }

            public void Connected()
            {
                _producer.Publish(_state);
            }
        }

        public sealed class Bulb : Client
        {
            public IPowerApi Power { get; } = new PowerImpl();

            public Bulb(Options options)
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
                var options = new OptionsBuilder()
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
                var options = new OptionsBuilder()
                    .WithDomain(Domains.Device)
                    .WithKind(Kinds.Bulb)
                    .WithId(23)
                    .WithSerializer(new Serializer())
                    .WithConnection(connection)
                    .WithLogger(Logging.Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                var proxy = ProxyFactory.CreateApiProxy<IPowerApi>(options);

                var reader = proxy.On.GetAsyncEnumerator(cancellationToken);

                await connection.ConnectAsync(CancellationToken.None);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await reader.MoveNextAsync();

                        var value = reader.Current;
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
                RunControllerAsync(cancellationTokenSource.Token)
            };

            Console.ReadLine();

            cancellationTokenSource.Cancel();

            Task.WhenAll(tasks);
        }
    }
}