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

    public interface IEntity
    {
        int Id { get; set; }
    }

    [Flags]
    public enum EntityFlags : byte
    {
        Normal = 0,
        Singleton = 1,
        ServerOnly = 2,
        LiveWhenOwnerGoAway = 4,
        AnyoneCanControl = 8,
        // TODO: ClientCanUpdateTrackableData
    }
}
