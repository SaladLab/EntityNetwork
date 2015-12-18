using System;
using TrackableData;
using UnityEngine;

namespace EntityNetwork.Unity3D
{
    public abstract class EntityNetworkBehaviour : MonoBehaviour, IClientEntity
    {
        public int Id { get; set; }
        public Type ProtoType { get; set; }
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

        public void SetTimerOnce(int timerId, TimeSpan delay, Action<IEntity, int> action)
        {
            Zone.TimerProvider.SetTimerOnce(Id, timerId, delay, action);
        }

        public void SetTimerRepeatedly(int timerId, TimeSpan interval, Action<IEntity, int> action)
        {
            Zone.TimerProvider.SetTimerRepeatedly(Id, timerId, interval, action);
        }

        public bool RemoveTimer(int timerId)
        {
            return Zone.TimerProvider.RemoveTimer(Id, timerId);
        }

        public void RemoveTimerAll()
        {
            Zone.TimerProvider.RemoveTimerAll(Id);
        }

        protected void SendInvoke(IInvokePayload payload)
        {
            Zone.Invoke(Id, payload);
        }
    }
}
