using System;
using System.Collections.Generic;
using System.Linq;
using TrackableData;

namespace EntityNetwork
{
    public class ServerZone : IServerZone, IChannelToServerZone
    {
        private readonly IServerEntityFactory _entityFactory;

        private int _lastEntityId;
        private readonly Dictionary<int, IServerEntity> _entityMap = new Dictionary<int, IServerEntity>();

        private readonly Dictionary<int, ProtobufChannelToClientZoneOutbound> _clientChannelMap = 
            new Dictionary<int, ProtobufChannelToClientZoneOutbound>();

        public ServerZone(IServerEntityFactory entityFactory)
        {
            _entityFactory = entityFactory;
        }

        public IServerEntity Spawn(Type protoTypeType, int ownerId, EntityFlags flags)
        {
            var entityId = _lastEntityId += 1;

            // create server entity

            var entity = _entityFactory.Create(protoTypeType);
            entity.Id = entityId;
            entity.ProtoTypeType = protoTypeType;
            entity.Zone = this;
            entity.OwnerId = ownerId;
            entity.Flags = flags;

            _entityMap.Add(entityId, entity);
            entity.OnSpawn();

            // start tracking trackable-data changes

            for (var i = 0; i < entity.TrackableDataCount; i++)
            {
                var iLocal = i;
                entity.GetTrackableData(i).SetDefaultTracker();
                entity.GetTrackableData(i).Tracker.HasChangeSet += 
                    _ => { OnEntityTrackableHasChangeSet(entityId, iLocal); };
            }

            // propagate this to clients

            var snapshot = entity.Snapshot;
            var trackables = entity.TrackableDataCount > 0 ? new ITrackable[entity.TrackableDataCount] : null;
            for (var i = 0; i < entity.TrackableDataCount; i++)
                trackables[i] = entity.GetTrackableData(i);

            foreach (var clientChannel in _clientChannelMap.Values)
            {
                // TODO: make batch api & use it for reducing network bandwidth in UNET
                clientChannel.Spawn(entityId, protoTypeType, ownerId, flags,
                                    snapshot, trackables);
            }

            return entity;
        }

        public bool Despawn(int id)
        {
            var entity = GetEntity(id);
            if (entity == null)
                return false;

            entity.OnDespawn();

            _entityMap.Remove(id);
            _entityFactory.Delete(entity);

            // propagate this to clients

            foreach (var clientChannel in _clientChannelMap.Values)
                clientChannel.Despawn(id);

            return true;
        }

        public IServerEntity GetEntity(int entityId)
        {
            IServerEntity entity;
            return _entityMap.TryGetValue(entityId, out entity) ? entity : null;
        }

        public IEnumerable<IServerEntity> GetEntities()
        {
            return _entityMap.Values;
        }

        private void OnEntityTrackableHasChangeSet(int entityId, int trackableDataIndex)
        {
            // TODO: Delayed ?

            var serverEntity = GetEntity(entityId);
            var trackable = serverEntity.GetTrackableData(trackableDataIndex);
            if (trackable.Changed)
            {
                foreach (var clientZone in _clientChannelMap.Values)
                    clientZone.UpdateChange(entityId, trackableDataIndex, trackable.Tracker);
            }
        }

        void IZone.Invoke(int entityId, IInvokePayload payload)
        {
            foreach (var clientZone in _clientChannelMap.Values)
                clientZone.Invoke(entityId, payload);
        }

        void IChannelToServerZone.Invoke(int entityId, IInvokePayload payload)
        {
            var serverEntity = GetEntity(entityId);
            if (serverEntity != null)
            {
                foreach (var channel in _clientChannelMap.Values)
                    channel.Begin();

                if ((payload.Flags & PayloadFlags.PassThrough) != 0)
                    ((IServerZone)this).Invoke(entityId, payload);
                else
                    payload.InvokeServer((IEntityServerHandler)serverEntity);

                foreach (var channel in _clientChannelMap.Values)
                    channel.End();
            }
        }

        void IChannelToServerZone.UpdateChange(int entityId, int trackableDataIndex, ITracker tracker)
        {
            // TODO: NOT IMPLEMENTED
        }

        public bool AddClient(int clientId, ProtobufChannelToClientZoneOutbound channelToClientZone)
        {
            if (_clientChannelMap.ContainsKey(clientId))
                return false;

            _clientChannelMap.Add(clientId, channelToClientZone);

            foreach (var entity in _entityMap.Values)
            {
                channelToClientZone.Begin();

                var trackables = entity.TrackableDataCount > 0 ? new ITrackable[entity.TrackableDataCount] : null;
                for (var i = 0; i < entity.TrackableDataCount; i++)
                    trackables[i] = entity.GetTrackableData(i);

                channelToClientZone.Spawn(entity.Id, entity.ProtoTypeType, entity.OwnerId, entity.Flags,
                                          entity.Snapshot, trackables);

                channelToClientZone.End();
            }
            return true;
        }

        public bool RemoveClient(int clientId)
        {
            if (_clientChannelMap.ContainsKey(clientId) == false)
                return false;

            _clientChannelMap.Remove(clientId);

            // Remove all entity that owned by removed client

            var removingIds = _entityMap.Where(x => (x.Value.OwnerId == clientId) &&
                                                    (x.Value.Flags & EntityFlags.LiveWhenOwnerGoAway) == 0)
                                        .Select(x => x.Key).ToList();
            foreach (var id in removingIds)
                Despawn(id);

            // TODO: Handover ownership to others

            var handOverIds = _entityMap.Where(x => (x.Value.OwnerId == clientId) &&
                                                    (x.Value.Flags & EntityFlags.LiveWhenOwnerGoAway) != 0)
                                        .Select(x => x.Key).ToList();

            return true;
        }

        public void RunAction(Action<ServerZone> action)
        {
            foreach (var channel in _clientChannelMap.Values)
                channel.Begin();

            action(this);

            foreach (var channel in _clientChannelMap.Values)
                channel.End();
        }
    }
}
