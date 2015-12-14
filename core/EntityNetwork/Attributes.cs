using System;

namespace EntityNetwork
{
    public sealed class PassThroughAttribute : Attribute
    {
    }

    public sealed class ToServerAttribute : Attribute
    {
    }

    public sealed class ToClientAttribute : Attribute
    {
    }

    public sealed class AnyoneCanCallAttribute : Attribute
    {
    }
}
