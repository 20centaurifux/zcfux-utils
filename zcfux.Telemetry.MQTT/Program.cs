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
            [Out(Topic = "status", Retain = false)]
            IAsyncEnumerable<bool> On { get; }

            [In(Topic = "status")]
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
                _producer.Publish(true);
            }
        }

        public sealed class Bulb : Client, IBulb
        {
            public IPowerApi Power { get; } = new PowerImpl();

            public Bulb(Telemetry.Device.Options options)
                : base(options)
            {
            }

            public void Test()
            {
                Power.ChangeStatus(true);
            }
        }

        static void Main(string[] args)
        {
            var connectionOptions = new ConnectionOptionsBuilder()
                .WithClientOptions(new ClientOptionsBuilder()
                    .WithLastWill(new LastWillOptionsBuilder()
                        .WithDomain(Domains.Device)
                        .WithKind(Kinds.Bulb)
                        .WithId(23)
                        .WithMessageOptions(new MessageOptions(Retain: true, TimeToLive: TimeSpan.Zero))
                        .Build())
                    .Build())
                .WithMessageQueue(new MemoryMessageQueue(50))
                .WithLogger(Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .WithCleanupRetainedMessages()
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                connection.ConnectAsync().Wait();

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
                    Console.ReadLine();
                }

                Console.WriteLine("Dispose");
            }
        }
    }
}