using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;

namespace Unity.Program.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (typeof(IUser) == null)
                throw new Exception("Force interface module to be loaded");

            var system = ActorSystem.Create("MySystem");
            DeadRequestProcessingActor.Install(system);

            var gateway = StartListen(system, ChannelType.Tcp, new IPEndPoint(IPAddress.Any, 5000)).Result;

            Console.WriteLine("Please enter key to quit.");
            Console.ReadLine();

            gateway.Stop().Wait();
            system.Terminate().Wait();
        }

        private static async Task<GatewayRef> StartListen(ActorSystem system, ChannelType channelType, IPEndPoint listenEndPoint)
        {
            var serializer = PacketSerializer.CreatePacketSerializer();

            var initiator = new GatewayInitiator
            {
                ListenEndPoint = listenEndPoint,
                GatewayLogger = LogManager.GetLogger($"Gateway({channelType})"),
                CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                PacketSerializer = serializer,
                CreateInitialActors = (context, connection) => new[]
                {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new UserActor(context.Self.Cast<ActorBoundChannelRef>(), CreateUserId()))),
                        new TaggedType[] { typeof(IUser) },
                        ActorBindingFlags.StopThenCloseChannel)
                }
            };

            var gateway = (channelType == ChannelType.Tcp)
                ? system.ActorOf(Props.Create(() => new TcpGateway(initiator)), "TcpGateway").Cast<GatewayRef>()
                : system.ActorOf(Props.Create(() => new UdpGateway(initiator)), "UdpGateway").Cast<GatewayRef>();

            await gateway.Start();
            return gateway;
        }

        private static int _lastUserId = 0;

        private static string CreateUserId()
        {
            var id = ++_lastUserId;
            return "User:" + id;
        }
    }
}
