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
            EntitySpawned = OnSpawn;
            EntityDespawned = OnDespawn;
            EntityInvalidOwnershipInvoked = OnInvalidOwnershipInvoke;
        }

        private void OnSpawn(IServerEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Spawn"));
        }

        private void OnDespawn(IServerEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Despawn"));
        }

        private void OnInvalidOwnershipInvoke(int clientId, IServerEntity entity, IInvokePayload payload)
        {
            Logs.Add(Tuple.Create(entity.Id, "InvalidOwnershipInvoke"));
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
            EntitySpawned = OnSpawn;
            EntityDespawned = OnDespawn;
        }

        private void OnSpawn(IClientEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Spawn"));
        }

        private void OnDespawn(IClientEntity entity)
        {
            Logs.Add(Tuple.Create(entity.Id, "Despawn"));
        }
    }
}
