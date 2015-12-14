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
        LiveWhenOwnerGoAway = 1,
        AnyoneCanControl = 2,
        ClientCanUpdateTrackableData = 4, // TODO: Implement
    }
}
