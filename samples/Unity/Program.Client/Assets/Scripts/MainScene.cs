using System.Collections;
using System.Net;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Client;
using Domain;
using EntityNetwork;
using TypeAlias;
using UnityEngine;

public class MainScene : MonoBehaviour, IGameObserver, IByteChannel
{
    private ClientZone _zone;
    private ProtobufChannelToClientZoneInbound _zoneChannel;
    private GameClientRef _gameClient;
    private GameObserver _gameObserver;

    private void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("Entities").transform;

        StartCoroutine(ProcessConnectToServer());
    }

    private IEnumerator ProcessConnectToServer()
    {
        WriteLine("Connect");

        G.Comm = CommunicatorHelper.CreateCommunicator<DomainProtobufSerializer>(
            G.Logger, new IPEndPoint(IPAddress.Loopback, 5000));
        G.Comm.Start();

        // get user

        var user = G.Comm.CreateRef<UserRef>();

        var t1 = user.GetId();
        yield return t1.WaitHandle;
        ShowResult(t1, "GetId()");

        if (t1.Status != TaskStatus.RanToCompletion)
            yield break;

        _gameObserver = (GameObserver)G.Comm.CreateObserver<IGameObserver>(this, startPending: true);

        // enter game

        var t2 = user.EnterGame("Test", _gameObserver);
        yield return t2.WaitHandle;
        ShowResult(t2, "EnterGame()");

        if (t2.Status != TaskStatus.RanToCompletion)
        {
            _gameObserver.Dispose();
            _gameObserver = null;
            yield break;
        }

        _gameClient = (GameClientRef)t2.Result.Item1;

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

        _gameObserver.GetEventDispatcher().Pending = false;
    }

    private void WriteLine(string text)
    {
        Debug.Log(text);
    }

    private void ShowResult(Task task, string name)
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

    private void ShowResult<TResult>(Task<TResult> task, string name)
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

    void IByteChannel.Write(byte[] bytes)
    {
        _gameClient.ZoneChange(null, bytes);
    }
}
