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

        void IChannelToClientZone.Spawn(int entityId, Type protoTypeType, int ownerId, EntityFlags flags,
                                        object snapshot, ITrackable[] trackables)
        {
            var entity = _entityFactory.Create(protoTypeType);

            entity.Id = entityId;
            entity.Zone = this;
            entity.OwnerId = ownerId;
            entity.Flags = flags;

            if (snapshot != null)
            {
                entity.Snapshot = snapshot;
            }

            if (trackables != null)
            {
                for (int i = 0; i < trackables.Length; i++)
                    entity.SetTrackableData(i, trackables[i]);
            }

            _entityMap.Add(entityId, entity);

            entity.OnSpawn();
        }

        void IChannelToClientZone.Despawn(int entityId)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
            {
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

        void IChannelToClientZone.UpdateChange(int entityId, int trackableDataIndex, ITracker tracker)
        {
            var entity = GetEntity(entityId);
            if (entity != null)
            {
                var trackable = entity.GetTrackableData(trackableDataIndex);
                if (trackable != null)
                {
                    entity.OnTrackableDataChanging(trackableDataIndex, tracker);

                    tracker.ApplyTo(trackable);

                    entity.OnTrackableDataChanged(trackableDataIndex, tracker);
                }
            }
        }

        public void RunAction(Action<ClientZone> action)
        {
            _serverChannel.Begin();

            action(this);

            _serverChannel.End();
        }
    }
}
