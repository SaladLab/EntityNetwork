using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;
using Newtonsoft.Json;

namespace Unity.Program.Server
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IGame, IGameClient
    {
        private class UserData
        {
            public UserRef UserActor;
            public GameObserver Observer;
        }

        private ILog _logger;
        private string _name;
        private Dictionary<string, UserData> _userMap;

        public GameActor(string name)
        {
            _logger = LogManager.GetLogger(string.Format("RoomActor({0})", name));
            _name = name;
            _userMap = new Dictionary<string, UserData>();
        }

        private void NotifyToAllObservers(Action<GameObserver> notifyAction)
        {
            foreach (var item in _userMap)
            {
                if (item.Value.Observer != null)
                    notifyAction(item.Value.Observer);
            }
        }

        async Task<GameInfo> IGame.Enter(string userId, IGameObserver observer)
        {
            if (_userMap.ContainsKey(userId))
                throw new InvalidOperationException();

            NotifyToAllObservers(o => o.Enter(userId));

            _userMap[userId] = new UserData
            {
                UserActor = new UserRef(Sender, this, null),
                Observer = (GameObserver)observer
            };

            return new GameInfo
            {
                Name = _name,
                Users = _userMap.Keys.ToList(),
            };
        }

        async Task IGame.Leave(string userId)
        {
            if (_userMap.ContainsKey(userId) == false)
                throw new InvalidOperationException();

            _userMap.Remove(userId);

            NotifyToAllObservers(o => o.Leave(userId));
        }

        async Task IGameClient.ZoneChange(byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}
