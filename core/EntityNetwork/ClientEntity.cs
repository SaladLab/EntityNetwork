using TrackableData;

namespace EntityNetwork
{
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

        public virtual object Snapshot
        {
            set { }
        }

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
}
