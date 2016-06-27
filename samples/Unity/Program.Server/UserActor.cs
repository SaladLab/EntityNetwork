using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;

namespace Unity.Program.Server
{
    [Log]
    public class UserActor : InterfacedActor, IUser
    {
        private ILog _logger;
        private readonly ActorBoundChannelRef _channel;
        private string _id;
        private GameRef _enteredGame;

        public UserActor(ActorBoundChannelRef channel, string id)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _channel = channel;
            _id = id;
        }

        protected override void PostStop()
        {
            UnlinkAll();
            base.PostStop();
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
            var game = actor.Cast<GameRef>().WithRequestWaiter(this);

            // enter the game

            var join = await game.Enter(_id, observer);

            // Bind an game actor to channel

            var boundActor = await _channel.BindActor(game.CastToIActorRef(),
                                                      new[] { new TaggedType(typeof(IGameClient), _id) });
            if (boundActor == null)
            {
                await game.Leave(_id);
                _logger.Error($"Failed in binding GameClient");
                throw new InvalidOperationException();
            }

            _enteredGame = game;
            return Tuple.Create((IGameClient)boundActor.Cast<GameClientRef>(), join.Item1, join.Item2);
        }

        async Task IUser.LeaveGame()
        {
            if (_enteredGame == null)
                throw new InvalidOperationException();

            // Let's exit from the room !

            await _enteredGame.Leave(_id);

            // Unbind an game actor from channel

            _channel.WithNoReply().UnbindActor(_enteredGame.CastToIActorRef());
            _enteredGame = null;
        }
    }
}
