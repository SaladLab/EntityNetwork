using System;
using System.Collections.Generic;
using TrackableData;

namespace EntityNetwork
{
    public interface IZone
    {
        IEntity GetEntity(int entityId);
        void Invoke(int entityId, IInvokePayload payload);
        TimeSpan GetTime();
    }

    public interface IServerZone : IZone
    {
        IServerEntity Spawn(Type protoType, int ownerId, EntityFlags flags, object param);
        bool Despawn(int id);
        new IServerEntity GetEntity(int entityId);
        IServerEntity GetEntity(Type protoType);
        T GetEntity<T>() where T : class, IServerEntity;
        IEnumerable<IServerEntity> GetEntities();
        IEnumerable<IServerEntity> GetEntities(Type protoType);
        IEnumerable<T> GetEntities<T>() where T : class, IServerEntity;
        IEntityTimerProvider TimerProvider { get; }
    }

    public interface IEntityTimerProvider
    {
        void SetTimerOnce(int entityId, int timerId, TimeSpan delay, Action<IEntity, int> action);
        void SetTimerRepeatedly(int entityId, int timerId, TimeSpan interval, Action<IEntity, int> action);
        bool RemoveTimer(int entityId, int timerId);
        void RemoveTimerAll(int entityId);
    }

    public interface IClientZone : IZone
    {
        int ClientId { get; }
        new IClientEntity GetEntity(int entityId);
        IClientEntity GetEntity(Type protoType);
        T GetEntity<T>() where T : class, IClientEntity;
        IEnumerable<IClientEntity> GetEntities();
        IEnumerable<IClientEntity> GetEntities(Type protoType);
        IEnumerable<T> GetEntities<T>() where T : class, IClientEntity;
        IEntityTimerProvider TimerProvider { get; }
    }

    public interface IChannelToServerZone
    {
        void Invoke(int clientId, int entityId, IInvokePayload payload);
        void UpdateChange(int clientId, int entityId, int trackableDataIndex, ITracker tracker);
    }

    public interface IChannelToClientZone
    {
        void Init(int clientId, DateTime startTime, TimeSpan elapsedTime);
        void Spawn(int entityId, Type protoType, int ownerId, EntityFlags flags, ISpawnPayload payload);
        void Despawn(int entityId);
        void Invoke(int entityId, IInvokePayload payload);
        void UpdateChange(int entityId, IUpdateChangePayload payload);
        void OwnershipChange(int entityId, int ownerId);
    }
}
