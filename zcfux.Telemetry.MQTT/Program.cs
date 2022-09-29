using zcfux.Logging;
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
            void ChangeStatus(bool on);
        }

        public interface IBulb
        {
            IPowerApi Power { get; }

            void Test();
        }

        sealed class PowerImpl : IPowerApi, IConnected
        {
            bool _state;

            readonly Producer<bool> _producer = new();

            public IAsyncEnumerable<bool> On => _producer;

            public void ChangeStatus(bool on)
            {
                _state = on;

                _producer.Publish(on);
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

        static void Main(string[] args)
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
                .WithLogger(Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .WithCleanupRetainedMessages()
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                var deviceOptions = new OptionsBuilder()
                    .WithDomain(Domains.Device)
                    .WithKind(Kinds.Bulb)
                    .WithId(23)
                    .WithConnection(connection)
                    .WithSerializer(new Serializer())
                    .WithLogger(Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                using (var bulb = new Bulb(deviceOptions))
                {
                    connection.ConnectAsync().Wait();

                    Console.ReadLine();
                }

                Console.WriteLine("Dispose");
            }
        }
    }
}