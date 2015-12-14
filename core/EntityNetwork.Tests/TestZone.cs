using System;
using System.Collections.Generic;

namespace EntityNetwork.Tests
{
    internal class TestServerZone : ServerZone
    {
        public List<Tuple<int, string>> Logs = new List<Tuple<int, string>>();

        public Tuple<int, string> Log(int index)
        {
            return index >= 0 ? Logs[index] : Logs[Logs.Count + index];
        }

        public TestServerZone(IServerEntityFactory entityFactory)
            : base(entityFactory)
        {
        }

        protected override void OnSpawn(IServerEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Spawn"));

        }

        protected override void OnDespawn(IServerEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Despawn"));
        }
    }

    internal class TestClientZone : ClientZone
    {
        public List<Tuple<int, string>> Logs = new List<Tuple<int, string>>();

        public Tuple<int, string> Log(int index)
        {
            return index >= 0 ? Logs[index] : Logs[Logs.Count + index];
        }

        public TestClientZone(IClientEntityFactory entityFactory, ProtobufChannelToServerZoneOutbound serverChannel)
            : base(entityFactory, serverChannel)
        {
        }

        protected override void OnSpawn(IClientEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Spawn"));
        }

        protected override void OnDespawn(IClientEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Despawn"));
        }
    }
}
