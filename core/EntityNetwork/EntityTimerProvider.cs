using System;
using System.Collections.Generic;
using System.Linq;
using TrackableData;

namespace EntityNetwork
{
    public class EntityTimerProvider : IEntityTimerProvider
    {
        private struct ScheduledWork
        {
            public int EntityId;
            public int TimerId;
            public TimeSpan Time;
            public TimeSpan Interval;
            public Action<IEntity, int> Action;
        }

        private readonly IZone _zone;
        private readonly List<ScheduledWork> _works = new List<ScheduledWork>();
        private bool _inProcessingWork;

        public Action<TimeSpan, Action> ActionScheduled;

        public EntityTimerProvider(IZone zone)
        {
            _zone = zone;
        }

        public void ProcessWork()
        {
            ProcessWork(GetTime());
        }

        private void ProcessWork(TimeSpan now)
        {
            try
            {
                _inProcessingWork = true;

                while (_works.Any())
                {
                    var work = _works[0];
                    if (work.Time > now)
                    {
                        ActionScheduled?.Invoke(work.Time - now, ProcessWork);
                        return;
                    }

                    _works.RemoveAt(0);

                    var entity = _zone.GetEntity(work.EntityId);
                    if (entity != null)
                    {
                        work.Action(entity, work.TimerId);
                        if (work.Interval > TimeSpan.Zero)
                        {
                            work.Time = now + work.Interval;
                            QueueWork(work);
                        }
                    }
                }
            }
            finally
            {
                _inProcessingWork = false;
            }
        }

        private TimeSpan GetTime()
        {
            return _zone.GetTime();
        }

        private void QueueWork(ScheduledWork work)
        {
            // TODO: Use a binary-search for finding upper-bound index to insert new item

            var index = _works.FindIndex(w => w.Time > work.Time);
            if (index == -1)
                index = _works.Count;

            _works.Insert(index, work);

            if (index == 0 && _inProcessingWork == false)
            {
                ActionScheduled?.Invoke(work.Time - GetTime(), ProcessWork);
            }
        }

        public void SetTimerOnce(int entityId, int timerId, TimeSpan delay, Action<IEntity, int> action)
        {
            RemoveTimer(entityId, timerId);

            QueueWork(new ScheduledWork
            {
                EntityId = entityId,
                TimerId = timerId,
                Time = GetTime() + delay,
                Action = action,
            });
        }

        public void SetTimerRepeatedly(int entityId, int timerId, TimeSpan interval, Action<IEntity, int> action)
        {
            RemoveTimer(entityId, timerId);

            QueueWork(new ScheduledWork
            {
                EntityId = entityId,
                TimerId = timerId,
                Time = GetTime() + interval,
                Interval = interval,
                Action = action,
            });
        }

        public bool RemoveTimer(int entityId, int timerId)
        {
            for (int i = 0; i < _works.Count; i++)
            {
                var work = _works[i];
                if (work.EntityId == entityId && work.TimerId == timerId)
                {
                    _works.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void RemoveTimerAll(int entityId)
        {
            for (int i = 0; i < _works.Count; i++)
            {
                var work = _works[i];
                if (work.EntityId == entityId)
                {
                    _works.RemoveAt(i);
                    i -= 1;
                }
            }
        }
    }
}
