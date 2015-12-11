using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityNetwork;
using ProtoBuf.Meta;
using TrackableData;
using TypeAlias;

namespace Basic
{
    class DummyChannelToServerZoneInbound : ByteChannel
    {
        public ProtobufChannelToServerZoneInbound Channel;

        public void Write(byte[] bytes)
        {
            ((ServerZone)Channel.InboundServerZone).RunAction(_ => { Channel.Write(bytes); });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var typeTable = new TypeAliasTable();

            var typeModel = TypeModel.Create();
            typeModel.Add(typeof(TrackablePocoTracker<ISpaceShipData>), false)
                     .SetSurrogate(typeof(TrackableSpaceShipDataTrackerSurrogate));

            var serverZone =  new ServerZone(EntityFactory.Default);

            var clientZones = Enumerable.Range(0, 2).Select(i =>
            {
                var channelUp = new ProtobufChannelToServerZoneOutbound
                {
                    TypeTable = typeTable,
                    TypeModel = typeModel,
                    OutboundChannel = new DummyChannelToServerZoneInbound
                    {
                        Channel = new ProtobufChannelToServerZoneInbound
                        {
                            TypeTable = typeTable,
                            TypeModel = typeModel,
                            InboundServerZone = serverZone,
                        }
                    }
                };
                var clientZone = new ClientZone(EntityFactory.Default, channelUp);
                var channel = new ProtobufChannelToClientZoneOutbound
                {
                    TypeTable = typeTable,
                    TypeModel = typeModel,
                    OutboundChannel = new ProtobufChannelToClientZoneInbound()
                    {
                        TypeTable = typeTable,
                        TypeModel = typeModel,
                        InboundClientZone = clientZone,
                    }
                };
                serverZone.AddClient(i, channel);
                return clientZone;
            }).ToArray();

            serverZone.RunAction(zone =>
            {
                zone.Spawn(typeof(ISpaceShip), 0, EntityFlags.Normal);
            });

            var cship1A = (ClientSpaceShip)clientZones[0].GetEntity(1);
            var cship1B = (ClientSpaceShip)clientZones[1].GetEntity(1);

            Console.WriteLine($"cship1A.Score = {cship1A.Data.Score}");
            clientZones[0].RunAction(_ => cship1A.Say("Hello"));
            Console.WriteLine($"cship1A.Score = {cship1A.Data.Score}");
            clientZones[0].RunAction(_ => cship1A.Stop(1, 2));
            clientZones[1].RunAction(_ =>
            {
                cship1B.Say("World");
                cship1B.Shoot(1, 2, 3, 4);
            });
        }
    }
}
