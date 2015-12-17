using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityNetwork
{
    public class ClientZone : IClientZone, IChannelToClientZone
    {
        private readonly IClientEntityFactory _entityFactory;
        private readonly Dictionary<int, IClientEntity> _entityMap = new Dictionary<int, IClientEntity>();
        private readonly ProtobufChannelToServerZoneOutbound _serverChannel;
        private int _clientId;
        private DateTime _startTime;
        private TimeSpan _timeOffset;

        public int ClientId => _clientId;

        public Action<IClientEntity> EntitySpawned;
        public Action<IClientEntity> EntityDespawned;

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

        public IClientEntity GetEntity(Type protoType)
        {
            return _entityMap.Values.FirstOrDefault(e => e.ProtoType == protoType);
        }

        public T GetEntity<T>() where T : class, IClientEntity
        {
            var protoType = _entityFactory.GetProtoType(typeof(T));
            if (protoType == null)
                throw new ArgumentException($"EntityType({nameof(T)}) doesn't have a prototype");

            return (T)GetEntity(protoType);
        }

        public IEnumerable<IClientEntity> GetEntities()
        {
            return _entityMap.Values;
        }

        public IEnumerable<IClientEntity> GetEntities(Type protoType)
        {
            return _entityMap.Values.Where(e => e.ProtoType == protoType);
        }

        public IEnumerable<T> GetEntities<T>() where T : class, IClientEntity
        {
            var protoType = _entityFactory.GetProtoType(typeof(T));
            if (protoType == null)
                throw new ArgumentException($"EntityType({nameof(T)}) doesn't have a prototype");

            return _entityMap.Values.Where(e => e.ProtoType == protoType).Cast<T>();
        }

        public TimeSpan GetTime()
        {
            return (DateTime.UtcNow - _startTime) + _timeOffset;
        }

        void IChannelToClientZone.Init(int clientId, DateTime startTime, TimeSpan elapsedTime)
        {
            _clientId = clientId;
            _startTime = startTime;
            _timeOffset = DateTime.UtcNow - (startTime + elapsedTime);
        }

        void IChannelToClientZone.Spawn(int entityId, Type protoType, int ownerId, EntityFlags flags,
                                        ISpawnPayload payload)
        {
            var entity = _entityFactory.Create(protoType);

            entity.Id = entityId;
            entity.ProtoType = protoType;
            entity.Zone = this;
            entity.OwnerId = ownerId;
            entity.Flags = flags;

            if (payload != null)
                payload.Notify(entity);

            _entityMap.Add(entityId, entity);

            entity.OnSpawn();
            EntitySpawned?.Invoke(entity);
        }

        void IChannelToClientZone.Despawn(int entityId)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
                return;

            EntityDespawned?.Invoke(entity);
            entity.OnDespawn();

            _entityMap.Remove(entityId);
            _entityFactory.Delete(entity);
        }

        void IZone.Invoke(int entityId, IInvokePayload payload)
        {
            _serverChannel.Invoke(0, entityId, payload);
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

        void IChannelToClientZone.OwnershipChange(int entityId, int ownerId)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
                entity.OwnerId = ownerId;
        }

        public void RunAction(Action<ClientZone> action)
        {
            _serverChannel.Begin();

            action(this);

            _serverChannel.End();
        }
    }
}
