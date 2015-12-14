using System;
using TrackableData;

namespace EntityNetwork
{
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

        void OnSpawn(object param);
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

        public virtual void OnSpawn(object param)
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
}
