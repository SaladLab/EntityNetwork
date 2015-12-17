using System;

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
        Singleton = 1,
        LiveWhenOwnerGoAway = 2,
        AnyoneCanControl = 4,
        // TODO: ClientCanUpdateTrackableData
    }
}
