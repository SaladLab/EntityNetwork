using System;
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
        IServerEntity Spawn(Type protoTypeType, int ownerId, EntityFlags flags);
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
        void Invoke(int entityId, IInvokePayload payload);
        void UpdateChange(int entityId, int trackableDataIndex, ITracker tracker);
    }

    public interface IChannelToClientZone
    {
        void Spawn(int entityId, Type protoTypeType, int ownerId, EntityFlags flags,
                   object snapshot, ITrackable[] trackables);
        void Despawn(int entityId);
        void Invoke(int entityId, IInvokePayload payload);
        void UpdateChange(int entityId, int trackableDataIndex, ITracker tracker);
    }
}
