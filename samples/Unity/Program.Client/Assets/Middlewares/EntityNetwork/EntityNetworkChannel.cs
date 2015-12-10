using System;
using System.IO;
using EntityNetwork;
using ProtoBuf.Meta;
using TypeAlias;
using UnityEngine.Networking;

public class EntityNetworkChannelToServerZone : ByteChannel
{
    public EntityNetworkClient NetworkClient;

    public void Write(byte[] bytes)
    {
        NetworkClient.CmdBuffer(bytes);
    }
}

public class EntityNetworkChannelToClientZone : ByteChannel
{
    public EntityNetworkClient NetworkClient;

    public void Write(byte[] bytes)
    {
        NetworkClient.RpcBuffer(bytes);
    }
}
