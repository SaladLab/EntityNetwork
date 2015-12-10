using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;

namespace Unity.Program.Server
{
    [Log]
    public class UserActor : InterfacedActor<UserActor>, IUser
    {
        private ILog _logger;
        private IActorRef _clientSession;
        private string _id;
        private GameRef _enteredGame;

        public UserActor(IActorRef clientSession, string id)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clientSession = clientSession;
            _id = id;
        }

        [MessageHandler]
        private void OnMessage(ActorBoundSessionMessage.SessionTerminated message)
        {
            // TODO: CALL MISSED!

            UnlinkAll();
            Context.Stop(Self);
        }

        private void UnlinkAll()
        {
            if (_enteredGame != null)
            {
                _enteredGame.WithNoReply().Leave(_id);
                _enteredGame = null;
            }
        }

        Task<string> IUser.GetId()
        {
            return Task.FromResult(_id);
        }

        async Task<Tuple<int, GameInfo>> IUser.EnterGame(string name, int observerId)
        {
            if (_enteredGame != null)
                throw new InvalidOperationException();

            // try to get game ref

            IActorRef actor;
            try
            {
                actor = await Context.ActorSelection("/user/game/" + name).ResolveOne(TimeSpan.Zero);
            }
            catch (ActorNotFoundException)
            {
                actor = Context.System.ActorOf(Props.Create(() => new GameActor(name)));
            }
            var game = new GameRef(actor, this, null);

            // enter the game

            var observer = new GameObserver(_clientSession, observerId);
            var info = await game.Enter(_id, observer);

            // Bind an occupant actor with client session

            var reply2 = await _clientSession.Ask<ActorBoundSessionMessage.BindReply>(
                new ActorBoundSessionMessage.Bind(game.Actor, typeof(IGameClient), _id));

            _enteredGame = game;
            return Tuple.Create(reply2.ActorId, info);
        }

        async Task IUser.LeaveGame()
        {
            if (_enteredGame == null)
                throw new InvalidOperationException();

            // Let's exit from the room !

            await _enteredGame.Leave(_id);

            // Unbind an occupant actor with client session

            _clientSession.Tell(new ActorBoundSessionMessage.Unbind(_enteredGame.Actor));

            _enteredGame = null;
        }
    }
}
