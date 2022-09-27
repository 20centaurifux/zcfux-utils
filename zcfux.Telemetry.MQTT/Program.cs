using zcfux.Logging;
using zcfux.Telemetry.Device;
using zcfux.Telemetry.MQTT.Device;

namespace zcfux.Telemetry.MQTT
{
    internal partial class Program
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
            var connectionOptions = new Device.OptionsBuilder()
                .WithDomain(Domains.Device)
                .WithKind(Kinds.Bulb)
                .WithId(23)
                .WithClientOptions(new ClientOptionsBuilder()
                    .WithoutTls()
                    .Build())
                .WithMessageQueue(new MemoryMessageQueue(1))
                .WithLogger(Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                .Build();

            using (var connection = new Connection(connectionOptions))
            {
                connection.ConnectAsync().Wait();

                var deviceOptions = new Telemetry.Device.OptionsBuilder()
                    .WithConnection(connection)
                    .WithSerializer(new Serializer())
                    .WithLogger(Factory.FromAssembly("zcfux.Logging", "zcfux.Logging.Console.Writer"))
                    .Build();

                using (var bulb = new Bulb(deviceOptions))
                {
                    Console.WriteLine("Disconnecting");

                    connection.DisconnectAsync().Wait();

                    Console.WriteLine("Test");

                    bulb.Test();

                    Console.ReadLine();

                    Console.WriteLine("Connecting");

                    connection.ConnectAsync().Wait();

                    Console.ReadLine();
                }

                Console.WriteLine("Dispose");
            }
        }
    }
}