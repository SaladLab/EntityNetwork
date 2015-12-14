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
        ClientCanUpdateTrackableData = 2, // TODO: Implement
    }
}
