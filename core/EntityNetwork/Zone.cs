﻿using System;
using System.Collections.Generic;
using TrackableData;

namespace EntityNetwork
{
    public interface IZone
    {
        void Invoke(int entityId, IInvokePayload payload);
    }

    public interface IServerZone : IZone
    {
        IServerEntity Spawn(Type protoTypeType, int ownerId, EntityFlags flags, object param);
        bool Despawn(int id);
        IServerEntity GetEntity(int entityId);
        IEnumerable<IServerEntity> GetEntities();
    }

    public interface IClientZone : IZone
    {
        IClientEntity GetEntity(int entityId);
    }

    public interface IChannelToServerZone
    {
        void Invoke(int clientId, int entityId, IInvokePayload payload);
        void UpdateChange(int clientId, int entityId, int trackableDataIndex, ITracker tracker);
    }

    public interface IChannelToClientZone
    {
        void Spawn(int entityId, Type protoTypeType, int ownerId, EntityFlags flags, ISpawnPayload payload);
        void Despawn(int entityId);
        void Invoke(int entityId, IInvokePayload payload);
        void UpdateChange(int entityId, IUpdateChangePayload payload);
        void OwnershipChange(int entityId, int ownerId);
    }
}
