using System;
using System.Collections.Generic;
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
    public class UserActor : InterfacedActor<UserActor>, IUser
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private long _id;
        private TrackableUserContext _userContext;
        private UserEventObserver _userEventObserver;
        private Dictionary<long, GameRef> _joinedGameMap;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession,
                         long id, TrackableUserContext userContext, int observerId)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
            _userContext = userContext;
            _userEventObserver = new UserEventObserver(clientSession, observerId);
            _joinedGameMap = new Dictionary<long, GameRef>();
        }

        private void UnlinkAll()
        {
            foreach (var game in _joinedGameMap.Values)
                game.WithNoReply().Leave(_id);
            _joinedGameMap.Clear();
        }

        [MessageHandler]
        protected void OnMessage(ActorBoundSessionMessage.SessionTerminated message)
        {
            UnlinkAll();
            Context.Stop(Self);
        }

        Task IUser.RegisterPairing(int observerId)
        {
            var observer = new UserPairingObserver(_clientSession, observerId);
            return _clusterContext.GamePairMaker.RegisterPairing(_id, _userContext.Data.Name, observer);
        }

        Task IUser.UnregisterPairing()
        {
            return _clusterContext.GamePairMaker.UnregisterPairing(_id);
        }

        Task<Tuple<int, int, GameInfo>> IUser.JoinGame(long gameId, int observerId)
        {
            throw new NotImplementedException();
        }

        Task IUser.LeaveGame(long gameId)
        {
            throw new NotImplementedException();
        }
    }
}
