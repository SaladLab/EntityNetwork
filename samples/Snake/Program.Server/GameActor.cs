using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
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
    }
}
