using System;
using System.Collections.Generic;
using TrackableData;

namespace EntityNetwork
{
    public class ClientZone : IClientZone, IChannelToClientZone
    {
        private readonly IClientEntityFactory _entityFactory;
        private readonly Dictionary<int, IClientEntity> _entityMap = new Dictionary<int, IClientEntity>();
        private readonly ProtobufChannelToServerZoneOutbound _serverChannel;

        public ClientZone(IClientEntityFactory entityFactory, ProtobufChannelToServerZoneOutbound serverChannel)
        {
            _entityFactory = entityFactory;
            _serverChannel = serverChannel;
        }

        public IClientEntity GetEntity(int entityId)
        {
            IClientEntity entity;
            return _entityMap.TryGetValue(entityId, out entity) ? entity : null;
        }

        void IChannelToClientZone.Spawn(int entityId, Type protoTypeType, int ownerId, EntityFlags flags, ISpawnPayload payload)
        {
            var entity = _entityFactory.Create(protoTypeType);

            entity.Id = entityId;
            entity.Zone = this;
            entity.OwnerId = ownerId;
            entity.Flags = flags;

            if (payload != null)
                payload.Notify(entity);

            _entityMap.Add(entityId, entity);

            entity.OnSpawn();
            OnSpawn(entity);
        }

        void IChannelToClientZone.Despawn(int entityId)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
            {
                OnDespawn(entity);
                entity.OnDespawn();

                _entityMap.Remove(entityId);
                _entityFactory.Delete(entity);
            }
        }

        void IZone.Invoke(int entityId, IInvokePayload payload)
        {
            _serverChannel.Invoke(entityId, payload);
        }

        void IChannelToClientZone.Invoke(int entityId, IInvokePayload payload)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
                payload.InvokeClient((IEntityClientHandler)entity);
        }

        void IChannelToClientZone.UpdateChange(int entityId, IUpdateChangePayload payload)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
                payload.Notify(entity);
        }

        public void RunAction(Action<ClientZone> action)
        {
            _serverChannel.Begin();

            action(this);

            _serverChannel.End();
        }

        protected virtual void OnSpawn(IClientEntity entity)
        {
        }

        protected virtual void OnDespawn(IClientEntity entity)
        {
        }
    }
}
