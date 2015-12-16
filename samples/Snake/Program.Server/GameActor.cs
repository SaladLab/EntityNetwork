using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IExtendedInterface<IGame, IGameClient>
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private long _id;
        private GameState _state;

        private class Player
        {
            public long UserId;
            public string UserName;
            public GameObserver Observer;
        }

        private List<Player> _players = new List<Player>();

        public GameActor(ClusterNodeContext clusterContext, long id, CreateGameParam param)
        {
            _logger = LogManager.GetLogger($"GameActor({id})");
            _clusterContext = clusterContext;
            _id = id;
        }

        private GameInfo GetGameInfo()
        {
            return new GameInfo
            {
                Id = _id,
                State = _state,
                PlayerNames = _players.Select(p => p.UserName).ToList(),
            };
        }

        private void NotifyToAllObservers(Action<int, GameObserver> notifyAction)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].Observer != null)
                    notifyAction(i + 1, _players[i].Observer);
            }
        }

        [ExtendedHandler]
        private Tuple<int, GameInfo> Join(long userId, string userName, int playerCount, IGameObserver observer)
        {
            if (_state != GameState.WaitingForPlayers)
                throw new ResultException(ResultCodeType.GameStarted);

            if (_players.Count > 2)
                throw new ResultException(ResultCodeType.GamePlayerFull);

            // TODO:

            return Tuple.Create(1, GetGameInfo());
        }

        [ExtendedHandler]
        private void Leave(long userId)
        {
            /*
            var playerId = GetPlayerId(userId);

            var player = _players[playerId - 1];
            _players[playerId - 1].Observer = null;

            NotifyToAllObservers((id, o) => o.Leave(playerId));

            if (_state != GameState.Ended)
            {
                // TODO: STATE
                _state = GameState.Aborted;
                NotifyToAllObservers((id, o) => o.Abort());
                NotifyToAllObserversForUserActor((id, o) => o.End(_id, GameResult.None));
            }

            if (_players.Count(p => p.Observer != null) == 0)
            {
                Self.Tell(InterfacedPoisonPill.Instance);
            }
            */
        }

        [ExtendedHandler]
        private void ZoneMessage(byte[] bytes, int clientId = 0)
        {
        }
    }
}
