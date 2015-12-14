using EntityNetwork;
using ProtoBuf.Meta;
using TypeAlias;
using UnityEngine;
using UnityEngine.Networking;

public class EntityNetworkManager : NetworkManager
{
    public static EntityNetworkManager Instance;

    public static TypeAliasTable TypeTable;
    public static TypeModel ProtobufTypeModel;

    private ServerZone _zone;
    private ProtobufChannelToServerZoneInbound _zoneChannel;

    public ServerZone Zone { get { return _zone; } }
    public ProtobufChannelToServerZoneInbound ZoneChannel { get { return _zoneChannel; } }

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        Debug.Log("EntityNetworkManager.OnStartServer");
        _zone = new ServerZone(EntityFactory.Default);
        _zoneChannel = new ProtobufChannelToServerZoneInbound
        {
            TypeTable = TypeTable,
            TypeModel = ProtobufTypeModel,
            InboundServerZone = _zone
        };
    }

    public override void OnStopServer()
    {
        Debug.Log("EntityNetworkManager.OnStopServer");
        _zone = null;
        _zoneChannel = null;
    }

    public bool AddClientToZone(int clientId, EntityNetworkClient networkClient)
    {
        var channel = new ProtobufChannelToClientZoneOutbound();
        channel.OutboundChannel = new EntityNetworkChannelToClientZone {NetworkClient = networkClient};
        channel.TypeTable = TypeTable;
        channel.TypeModel = ProtobufTypeModel;
        return _zone.AddClient(clientId, channel);
    }

    public void RemoveClientToZone(int id)
    {
        if (_zone == null)
            return;

        _zone.RemoveClient(id);
    }
}
