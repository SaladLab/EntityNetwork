using System;

namespace EntityNetwork
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PassThroughAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ToServerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ToClientAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AnyoneCanCallAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class SingletonAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ServerOnlyAttribute : Attribute
    {
    }
}
