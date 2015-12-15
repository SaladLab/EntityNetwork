using EntityNetwork;

public class EntityNetworkChannelToServerZone : IByteChannel
{
    public EntityNetworkClient NetworkClient;

    public void Write(byte[] bytes)
    {
        NetworkClient.CmdBuffer(bytes);
    }
}

public class EntityNetworkChannelToClientZone : IByteChannel
{
    public EntityNetworkClient NetworkClient;

    public void Write(byte[] bytes)
    {
        NetworkClient.RpcBuffer(bytes);
    }
}
