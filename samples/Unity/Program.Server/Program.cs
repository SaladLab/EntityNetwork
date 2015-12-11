using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;
using ProtoBuf.Meta;
using TypeAlias;

namespace Unity.Program.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            if (typeof(IUser) == null)
                throw new Exception("Force interface module to be loaded");

            var system = ActorSystem.Create("MySystem");
            DeadRequestProcessingActor.Install(system);

            StartListen(system, 5000);

            Console.WriteLine("Please enter key to quit.");
            Console.ReadLine();
        }

        private static TcpConnectionSettings _tcpConnectionSettings;

        private static void StartListen(ActorSystem system, int port)
        {
            var logger = LogManager.GetLogger("ClientGateway");

            _tcpConnectionSettings = new TcpConnectionSettings
            {
                PacketSerializer = new PacketSerializer(
                    new PacketSerializerBase.Data(
                        new ProtoBufMessageSerializer(TypeModel.Create()),
                        new TypeAliasTable()))
            };

            var clientGateway = system.ActorOf(Props.Create(() => new ClientGateway(logger, CreateSession)));
            clientGateway.Tell(new ClientGatewayMessage.Start(new IPEndPoint(IPAddress.Any, port)));
        }

        private static IActorRef CreateSession(IActorContext context, Socket socket)
        {
            var logger = LogManager.GetLogger($"Client({socket.RemoteEndPoint})");
            return context.ActorOf(Props.Create(() => new ClientSession(
                                                          logger, socket, _tcpConnectionSettings, CreateInitialActor)));
        }

        private static int _lastUserId = 0;
        private static string CreateUserId()
        {
            var id = ++_lastUserId;
            return "User:" + id;
        }

        private static Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context, Socket socket)
        {
            return new[]
            {
                Tuple.Create(context.System.ActorOf(Props.Create(() => new UserActor(context.Self, CreateUserId()))),
                             typeof(IUser)),
            };
        }
    }
}
