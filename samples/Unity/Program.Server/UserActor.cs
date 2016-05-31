using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace Unity.Program.Server
{
    [Log]
    public class UserActor : InterfacedActor, IUser
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

        async Task<Tuple<IGameClient, int, GameInfo>> IUser.EnterGame(string name, IGameObserver observer)
        {
            if (_enteredGame != null)
                throw new InvalidOperationException();

            // try to get game ref

            IActorRef actor;
            try
            {
                actor = await Context.ActorSelection("/user/game_" + name).ResolveOne(TimeSpan.Zero);
            }
            catch (ActorNotFoundException)
            {
                actor = Context.System.ActorOf(Props.Create(() => new GameActor(name)), "game_" + name);
            }
            var game = new GameRef(actor, this, null);

            // enter the game

            var join = await game.Enter(_id, observer);

            // Bind an occupant actor with client session

            var bind = await _clientSession.Ask<ActorBoundSessionMessage.BindReply>(
                new ActorBoundSessionMessage.Bind(game.Actor, typeof(IGameClient), _id));
            if (bind.ActorId == 0)
            {
                await game.Leave(_id);
                _logger.Error($"Failed in binding GameClient");
                throw new InvalidOperationException();
            }

            _enteredGame = game;
            return Tuple.Create((IGameClient)BoundActorRef.Create<GameClientRef>(bind.ActorId), join.Item1, join.Item2);
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
