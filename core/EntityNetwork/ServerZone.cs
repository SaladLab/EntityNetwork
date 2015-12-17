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

        private int _beginActionCount;
        private readonly HashSet<int> _changedEntitySet = new HashSet<int>();

        private readonly Dictionary<int, ProtobufChannelToClientZoneOutbound> _clientChannelMap =
            new Dictionary<int, ProtobufChannelToClientZoneOutbound>();

        private readonly DateTime _startTime;

        public Action<IServerEntity> EntitySpawned;
        public Action<IServerEntity> EntityDespawned;
        public Action<int, int, IInvokePayload> EntityInvalidTargetInvoked;
        public Action<int, IServerEntity, IInvokePayload> EntityInvalidOwnershipInvoked;

        public ServerZone(IServerEntityFactory entityFactory)
        {
            _entityFactory = entityFactory;
            _startTime = DateTime.UtcNow;
        }

        public IServerEntity Spawn(Type protoType, int ownerId, EntityFlags flags = EntityFlags.Normal,
                                   object param = null)
        {
            var entityId = _lastEntityId += 1;

            // check singleton condition

            if ((flags & EntityFlags.Singleton) != 0)
            {
                if (GetEntity(protoType) != null)
                    throw new InvalidOperationException($"Entity({protoType.Name}) should be singleton");
            }

            // create server entity

            var entity = _entityFactory.Create(protoType);
            if (entity == null)
                throw new InvalidOperationException($"EntityFactory cannot create entity from ProtoType({protoType.Name})");

            entity.Id = entityId;
            entity.ProtoType = protoType;
            entity.Zone = this;
            entity.OwnerId = ownerId;
            entity.Flags = flags;

            _entityMap.Add(entityId, entity);
            entity.OnSpawn(param);
            EntitySpawned?.Invoke(entity);

            // start tracking trackable-data changes

            for (var i = 0; i < entity.TrackableDataCount; i++)
            {
                var iLocal = i;
                entity.GetTrackableData(i).SetDefaultTracker();
                entity.GetTrackableData(i).Tracker.HasChangeSet +=
                    _ => { OnEntityTrackableHasChangeSet(entityId, iLocal); };
            }

            // propagate this to clients

            var payload = entity.GetSpawnPayload();
            foreach (var clientChannel in _clientChannelMap.Values)
            {
                // TODO: make batch api & use it for reducing network bandwidth in UNET
                clientChannel.Spawn(entityId, protoType, ownerId, flags, payload);
            }

            return entity;
        }

        public bool Despawn(int id)
        {
            var entity = GetEntity(id);
            if (entity == null)
                return false;

            EntityDespawned?.Invoke(entity);
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

        public IServerEntity GetEntity(Type protoType)
        {
            return _entityMap.Values.FirstOrDefault(e => e.ProtoType == protoType);
        }

        public T GetEntity<T>() where T : class, IServerEntity
        {
            var protoType = _entityFactory.GetProtoType(typeof(T));
            if (protoType == null)
                throw new ArgumentException($"EntityType({nameof(T)}) doesn't have a prototype");

            return (T)GetEntity(protoType);
        }

        public IEnumerable<IServerEntity> GetEntities()
        {
            return _entityMap.Values;
        }

        public IEnumerable<IServerEntity> GetEntities(Type protoType)
        {
            return _entityMap.Values.Where(e => e.ProtoType == protoType);
        }

        public IEnumerable<T> GetEntities<T>() where T : class, IServerEntity
        {
            var protoType = _entityFactory.GetProtoType(typeof(T));
            if (protoType == null)
                throw new ArgumentException($"EntityType({nameof(T)}) doesn't have a prototype");

            return _entityMap.Values.Where(e => e.ProtoType == protoType).Cast<T>();
        }

        public TimeSpan GetTime()
        {
            return DateTime.UtcNow - _startTime;
        }

        private void OnEntityTrackableHasChangeSet(int entityId, int trackableDataIndex)
        {
            _changedEntitySet.Add(entityId);
        }

        void IZone.Invoke(int entityId, IInvokePayload payload)
        {
            foreach (var clientZone in _clientChannelMap.Values)
                clientZone.Invoke(entityId, payload);
        }

        void IChannelToServerZone.Invoke(int clientId, int entityId, IInvokePayload payload)
        {
            var serverEntity = GetEntity(entityId);
            if (serverEntity == null)
            {
                EntityInvalidTargetInvoked?.Invoke(clientId, entityId, payload);
                return;
            }

            // Check Ownership
            if (((serverEntity.Flags & EntityFlags.AnyoneCanControl) == 0) && 
                ((payload.Flags & PayloadFlags.AnyoneCanCall) == 0) &&
                serverEntity.OwnerId != clientId)
            {
                EntityInvalidOwnershipInvoked?.Invoke(clientId, serverEntity, payload);
                return;
            }

            if ((payload.Flags & PayloadFlags.PassThrough) != 0)
                ((IServerZone)this).Invoke(entityId, payload);
            else
                payload.InvokeServer((IEntityServerHandler)serverEntity);
        }

        void IChannelToServerZone.UpdateChange(int clientId, int entityId, int trackableDataIndex, ITracker tracker)
        {
            // TODO: NOT IMPLEMENTED
        }

        public bool AddClient(int clientId, ProtobufChannelToClientZoneOutbound channelToClientZone)
        {
            if (clientId <= 0)
                throw new ArgumentException("ClientId should be greater than zero");

            if (_clientChannelMap.ContainsKey(clientId))
                return false;

            // If there are orphan entities,
            // make an incoming client own all orphan entities.

            if (_clientChannelMap.Count == 0)
            {
                foreach (var entity in _entityMap.Values)
                {
                    if (entity.OwnerId == -1)
                        entity.OwnerId = clientId;
                }
            }

            // Sync all entities to an incoming client

            _clientChannelMap.Add(clientId, channelToClientZone);

            channelToClientZone.Begin();
            channelToClientZone.Init(clientId, _startTime, DateTime.UtcNow - _startTime);

            foreach (var entity in _entityMap.Values)
            {
                var payload = entity.GetSpawnPayload();
                channelToClientZone.Spawn(entity.Id, entity.ProtoType, entity.OwnerId, entity.Flags, payload);
            }

            channelToClientZone.End();
            return true;
        }

        public bool RemoveClient(int clientId)
        {
            if (_clientChannelMap.ContainsKey(clientId) == false)
                return false;

            _clientChannelMap.Remove(clientId);

            BeginAction();

            // Remove all entity that owned by removed client

            var removingIds = _entityMap.Where(x => (x.Value.OwnerId == clientId) &&
                                                    (x.Value.Flags & EntityFlags.LiveWhenOwnerGoAway) == 0)
                                        .Select(x => x.Key).ToList();
            foreach (var id in removingIds)
                Despawn(id);

            // Handover ownership to others

            var handOverEntities = _entityMap.Where(x => (x.Value.OwnerId == clientId) &&
                                                         (x.Value.Flags & EntityFlags.LiveWhenOwnerGoAway) != 0)
                                             .Select(x => x.Value).ToList();
            if (handOverEntities.Count > 0)
            {
                if (_clientChannelMap.Count > 0)
                {
                    // If there are clients, give an ownership of entity left by round-robin fashion.

                    var clients = _clientChannelMap.ToList();
                    var clientIndex = 0;
                    foreach (var entity in handOverEntities)
                    {
                        entity.OwnerId = clients[clientIndex].Key;
                        clientIndex = (clientIndex + 1) % clients.Count;

                        foreach (var clientChannel in _clientChannelMap.Values)
                            clientChannel.OwnershipChange(entity.Id, entity.OwnerId);
                    }
                }
                else
                {
                    // If there is no client, make them orphan.

                    foreach (var entity in handOverEntities)
                    {
                        entity.OwnerId = -1;
                    }
                }
            }

            EndAction();
            return true;
        }

        public void SetEntityOwnership(int entityId, int ownerId)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
                throw new ArgumentException("Invalid entity: " + entityId);

            if (ownerId != 0 && _clientChannelMap.ContainsKey(ownerId) == false)
                throw new ArgumentException("Invalid ownerId: " + ownerId);

            if (entity.OwnerId == ownerId)
                return;

            entity.OwnerId = ownerId;

            foreach (var clientChannel in _clientChannelMap.Values)
                clientChannel.OwnershipChange(entity.Id, entity.OwnerId);
        }

        public void BeginAction()
        {
            _beginActionCount += 1;
            if (_beginActionCount > 1)
                return;

            foreach (var channel in _clientChannelMap.Values)
                channel.Begin();
        }

        public void EndAction()
        {
            if (_beginActionCount == 0)
                throw new InvalidOperationException("BeginAction required!");

            _beginActionCount -= 1;
            if (_beginActionCount > 0)
                return;

            // notify trackable-changes of entities to clients

            if (_changedEntitySet.Count > 0)
            {
                foreach (var entityId in _changedEntitySet)
                {
                    var entity = GetEntity(entityId);
                    if (entity != null)
                    {
                        var payload = entity.GetUpdateChangePayload();

                        foreach (var clientZone in _clientChannelMap.Values)
                            clientZone.UpdateChange(entityId, payload);

                        for (var i = 0; i < entity.TrackableDataCount; i++)
                            entity.GetTrackableData(i).Tracker.Clear();
                    }
                }
                _changedEntitySet.Clear();
            }

            foreach (var channel in _clientChannelMap.Values)
                channel.End();
        }

        public void RunAction(Action<ServerZone> action)
        {
            BeginAction();
            action(this);
            EndAction();
        }
    }
}
