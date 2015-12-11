using System;
using TrackableData;

namespace EntityNetwork
{
    public interface IEntityPrototype
    {
    }

    public interface IEntityServerHandler
    {
    }

    public interface IEntityClientHandler
    {
    }

    [Flags]
    public enum EntityFlags : byte
    {
        Normal = 0,
        LiveWhenOwnerGoAway = 1,
        ClientCanUpdateTrackableData = 2, // TODO: Implement
    }

    // TODO: Remove setter of properties

    public interface IServerEntity
    {
        int Id { get; set; }
        Type ProtoTypeType { get; set; }
        IServerZone Zone { get; set; }
        int OwnerId { get; set; }
        EntityFlags Flags { get; set; }

        object Snapshot { get; }

        int TrackableDataCount { get; }
        ITrackable GetTrackableData(int index);
        void SetTrackableData(int index, ITrackable trackable);

        ISpawnPayload GetSpawnPayload();
        IUpdateChangePayload GetUpdateChangePayload();

        void OnSpawn();
        void OnDespawn();
    }

    public abstract class ServerEntity : IServerEntity
    {
        public int Id { get; set; }
        public Type ProtoTypeType { get; set; }
        public IServerZone Zone { get; set; }
        public int OwnerId { get; set; }
        public EntityFlags Flags { get; set; }

        public virtual object Snapshot
        {
            get { return null; }
        }

        public abstract int TrackableDataCount { get; }
        public abstract ITrackable GetTrackableData(int index);
        public abstract void SetTrackableData(int index, ITrackable trackable);

        public virtual ISpawnPayload GetSpawnPayload()
        {
            return null;
        }

        public virtual IUpdateChangePayload GetUpdateChangePayload()
        {
            return null;
        }

        public virtual void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
        }

        protected void SendInvoke(IInvokePayload payload)
        {
            Zone.Invoke(Id, payload);
        }
    }

    // TODO: Remove setter of properties

    public interface IClientEntity
    {
        int Id { get; set; }
        IClientZone Zone { get; set; }
        int OwnerId { get; set; }
        EntityFlags Flags { get; set; }

        object Snapshot { set; }

        int TrackableDataCount { get; }
        ITrackable GetTrackableData(int index);
        void SetTrackableData(int index, ITrackable trackable);

        void OnTrackableDataChanging(int index, ITracker tracker);
        void OnTrackableDataChanged(int index, ITracker tracker);

        void OnSpawn();
        void OnDespawn();
    }

    public abstract class ClientEntity : IClientEntity
    {
        public int Id { get; set; }
        public IClientZone Zone { get; set; }
        public int OwnerId { get; set; }
        public EntityFlags Flags { get; set; }

        public virtual object Snapshot { set { } }

        public abstract int TrackableDataCount { get; }
        public abstract ITrackable GetTrackableData(int index);
        public abstract void SetTrackableData(int index, ITrackable trackable);

        public virtual void OnTrackableDataChanging(int index, ITracker tracker)
        {
        }

        public virtual void OnTrackableDataChanged(int index, ITracker tracker)
        {
        }

        public virtual void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
        }

        protected void SendInvoke(IInvokePayload payload)
        {
            Zone.Invoke(Id, payload);
        }
    }

    public interface IServerEntityFactory
    {
        IServerEntity Create(Type protoTypeType);
        void Delete(IServerEntity entity);
    }

    public interface IClientEntityFactory
    {
        IClientEntity Create(Type protoTypeType);
        void Delete(IClientEntity entity);
    }
}
