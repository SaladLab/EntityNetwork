using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using Domain.Entity;
using EntityNetwork;
using TrackableData;
using TypeAlias;

public class MainScene : MonoBehaviour, IGameObserver, ByteChannel
{
    private ClientZone _zone;
    private ProtobufChannelToClientZoneInbound _zoneChannel;
    private GameClientRef _gameClient;
    private ObserverEventDispatcher _gameObserver;

    void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("Canvas").transform;

        StartCoroutine(ProcessConnectToServer());
    }

    void Update()
    {
        if (_zone != null)
        {
            // make gameObserver work in main thread
            _gameObserver.Pending = false;
            _gameObserver.Pending = true;
        }
    }

    IEnumerator ProcessConnectToServer()
    {
        WriteLine("Connect");

        var serializer = new PacketSerializer(
            new PacketSerializerBase.Data(
                new ProtoBufMessageSerializer(new DomainProtobufSerializer()),
                new TypeAliasTable()));

        G.Comm = new Communicator(G.Logger, new IPEndPoint(IPAddress.Loopback, 5000),
            _ => new TcpConnection(serializer, LogManager.GetLogger("Connection")));
        G.Comm.Start();

        // get user

        var user = new UserRef(new SlimActorRef(1), new SlimRequestWaiter(G.Comm, this), null);

        var t1 = user.GetId();
        yield return t1.WaitHandle;
        ShowResult(t1, "GetId()");

        if (t1.Status != TaskStatus.RanToCompletion)
            yield break;

        var observerId = G.Comm.IssueObserverId();
        _gameObserver = new ObserverEventDispatcher(this, startPending: true);
        G.Comm.AddObserver(observerId, _gameObserver);

        // enter game

        var t2 = user.EnterGame("Test", observerId);
        yield return t2.WaitHandle;
        ShowResult(t2, "EnterGame()");

        if (t2.Status != TaskStatus.RanToCompletion)
            yield break;

        // TODO: MAKE OFFICIAL
        EntityNetworkClient.LocalClientId = t2.Result.Item2;

        _gameClient = new GameClientRef(
            new SlimActorRef(t2.Result.Item1),
            new SlimRequestWaiter(G.Comm, this), null);

        _zone = new ClientZone(
            ClientEntityFactory.Default,
            new ProtobufChannelToServerZoneOutbound
            {
                TypeTable = new TypeAliasTable(),
                TypeModel = new DomainProtobufSerializer(),
                OutboundChannel = this
            });

        _zoneChannel = new ProtobufChannelToClientZoneInbound
        {
            TypeTable = new TypeAliasTable(),
            TypeModel = new DomainProtobufSerializer(),
            InboundClientZone = _zone
        };
    }

    void WriteLine(string text)
    {
        Debug.Log(text);
    }

    void ShowResult(Task task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            WriteLine(string.Format("{0}: Done", name));
        else if (task.Status == TaskStatus.Faulted)
            WriteLine(string.Format("{0}: Exception = {1}", name, task.Exception));
        else if (task.Status == TaskStatus.Canceled)
            WriteLine(string.Format("{0}: Canceled", name));
        else
            WriteLine(string.Format("{0}: Illegal Status = {1}", name, task.Status));
    }

    void ShowResult<TResult>(Task<TResult> task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            WriteLine(string.Format("{0}: Result = {1}", name, task.Result));
        else
            ShowResult((Task)task, name);
    }

    void IGameObserver.Enter(string userId)
    {
        Debug.LogFormat("IGameObserver.Enter({0})", userId);
    }

    void IGameObserver.Leave(string userId)
    {
        Debug.LogFormat("IGameObserver.Leave({0})", userId);
    }

    void IGameObserver.ZoneChange(byte[] bytes)
    {
        Debug.LogFormat("IGameObserver.ZoneChange({0})", bytes.Length);
        _zoneChannel.Write(bytes);
    }

    void ByteChannel.Write(byte[] bytes)
    {
        _gameClient.ZoneChange(null, bytes);
    }
}
