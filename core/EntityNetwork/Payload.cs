using System;

namespace EntityNetwork
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PayloadTableForEntity : Attribute
    {
        public Type Type { get; private set; }

        public PayloadTableForEntity(Type type)
        {
            Type = type;
        }
    }

    [Flags]
    public enum PayloadFlags
    {
        PassThrough = 1,
        ToServer = 2,
        ToClient = 4,
        AnyoneCanCall = 8,
    }

    public interface IInvokePayload
    {
        PayloadFlags Flags { get; }
        /*
        Type GetInterfaceType();
        Type GetServerInterfaceType();
        Type GetClientInterfaceType();
        */
        void InvokeServer(IEntityServerHandler target);
        void InvokeClient(IEntityClientHandler target);
    }

    public interface ISpawnPayload
    {
        void Gather(IServerEntity entity);
        void Notify(IClientEntity entity);
    }

    public interface IUpdateChangePayload
    {
        void Gather(IServerEntity entity);
        void Notify(IClientEntity entity);
    }
}
