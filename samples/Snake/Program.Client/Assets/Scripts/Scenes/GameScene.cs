using System;
using System.Collections;
using Akka.Interfaced.SlimSocket.Client;
using Domain;
using TypeAlias;
using UnityEngine;
using UnityEngine.UI;

public class GameScene : MonoBehaviour, IUserPairingObserver, IGameObserver
{
    public RectTransform LoadingPanel;
    public RectTransform GamePanel;

    public Text LoadingText;
    public Transform GameEntityRoot;

    private Tuple<long, string> _pairedGame;
    private int _gameObserverId;
    private GameInfo _gameInfo;
    private GameClientRef _gameClient;

    protected void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameEntityRoot;

        var typeTable = new TypeAliasTable();
        EntityNetworkManager.TypeTable = typeTable;
        EntityNetworkManager.ProtobufTypeModel = new DomainProtobufSerializer();

        ApplicationComponent.TryInit();
        UiManager.Initialize();

        StartJoinGame();
    }

    private void StartJoinGame()
    {
        LoadingPanel.gameObject.SetActive(true);
        GamePanel.gameObject.SetActive(false);

        StartCoroutine(G.User == null
            ? ProcessLoginAndJoinGame()
            : ProcessJoinGame());
    }

    private IEnumerator ProcessLoginAndJoinGame()
    {
        var loginId = PlayerPrefs.GetString("LoginId");
        var loginPassword = PlayerPrefs.GetString("LoginPassword");

        // TEST
        loginId = "editor";
        loginPassword = "1234";

        if (string.IsNullOrEmpty(loginId))
        {
            UiMessageBox.ShowMessageBox("Cannot find id");
            yield break;
        }

        yield return StartCoroutine(ProcessLoginUser(loginId, loginPassword));
        if (G.User == null)
        {
            UiMessageBox.ShowMessageBox("Failed to login");
            yield break;
        }

        yield return StartCoroutine(ProcessJoinGame());
    }

    private IEnumerator ProcessLoginUser(string id, string password)
    {
        G.Logger.Info("ProcessLoginUser");

        var task = LoginProcessor.Login(G.ServerEndPoint, id, password, null);
        yield return task.WaitHandle;
    }

    private IEnumerator ProcessJoinGame()
    {
        G.Logger.Info("ProcessJoinGame");

        // Finding Game
        // Register user to pairing queue and waiting for 5 secs.

        LoadingText.text = "Finding Game...";

        _pairedGame = null;

        var observerId = G.Comm.IssueObserverId();
        G.Comm.AddObserver(observerId, new ObserverEventDispatcher(this));
        yield return G.User.RegisterPairing(observerId).WaitHandle;

        var startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalSeconds < 5 && _pairedGame == null)
        {
            yield return null;
        }

        G.Comm.RemoveObserver(observerId);
        if (_pairedGame == null)
        {
            yield return G.User.UnregisterPairing().WaitHandle;
            var box = UiMessageBox.ShowMessageBox("Cannot find game");
            yield return StartCoroutine(box.WaitForHide());
            Application.LoadLevel("MainScene");
            yield break;
        }

        // Join Game

        var roomId = _pairedGame.Item1;
        var observerId2 = G.Comm.IssueObserverId();
        var observer = new ObserverEventDispatcher(this, startPending: true, keepOrder: true);
        G.Comm.AddObserver(observerId2, observer);
        var joinRet = G.User.JoinGame(roomId, observerId2);
        yield return joinRet.WaitHandle;

        if (joinRet.Exception != null)
        {
            UiMessageBox.ShowMessageBox("Failed to join\n" + joinRet.Exception);
            G.Comm.RemoveObserver(observerId2);
            yield break;
        }

        _gameObserverId = observerId2;
        _gameInfo = joinRet.Result.Item3;
        /*
        _myPlayerId = joinRet.Result.Item2;
        */
        _gameClient = new GameClientRef(
            new SlimActorRef(joinRet.Result.Item1), G.SlimRequestWaiter, null);

        observer.Pending = false;
        LoadingText.text = "Waiting for " + _pairedGame.Item2 + "...";
    }

    void IUserPairingObserver.MakePair(long gameId, string opponentName)
    {
        Debug.Log(string.Format("IUserPairingObserver.MakePair {0} {1}", gameId, opponentName));
        _pairedGame = Tuple.Create(gameId, opponentName);
    }

    void IGameObserver.Join(long userId, string userName, int clientId)
    {
        Debug.Log(string.Format("IGameObserver.Join {0} {1} {2}", userId, userName, clientId));
    }

    void IGameObserver.Leave(long userId)
    {
        Debug.Log(string.Format("IGameObserver.Leave {0}", userId));
    }

    void IGameObserver.ZoneMessage(byte[] bytes)
    {
        Debug.Log(string.Format("IGameObserver.ZoneMessage {0}", bytes.Length));
    }

    public void OnLeftButtonClick()
    {
        OnKey(KeyCode.LeftArrow);
    }

    public void OnRightButtonClick()
    {
        OnKey(KeyCode.RightArrow);
    }

    public void OnUpButtonClick()
    {
        OnKey(KeyCode.UpArrow);
    }

    public void OnDownButtonClick()
    {
        OnKey(KeyCode.DownArrow);
    }

    private void OnKey(KeyCode code)
    {
        Debug.Log("OnKey " + code);
    }
}
