using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;
using Domain.Entity;
using EntityNetwork;
using Newtonsoft.Json;
using ProtoBuf.Meta;
using TypeAlias;

namespace Unity.Program.Server
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IGame, IGameClient
    {
        private class UserData : ByteChannel
        {
            public UserRef UserActor;
            public GameObserver Observer;
            public int ClientId;
            public ProtobufChannelToClientZoneOutbound OutboundChannel;
            public ProtobufChannelToServerZoneInbound InboundChannel;

            void ByteChannel.Write(byte[] bytes)
            {
                Observer.ZoneChange(bytes);
            }
        }

        private ILog _logger;
        private string _name;
        private Dictionary<string, UserData> _userMap;
        private ServerZone _zone;
        private int _lastClientId;

        public GameActor(string name)
        {
            _logger = LogManager.GetLogger(string.Format("RoomActor({0})", name));
            _name = name;
            _userMap = new Dictionary<string, UserData>();
            _zone = new ServerZone(EntityFactory.Default);
        }

        private void NotifyToAllObservers(Action<GameObserver> notifyAction)
        {
            foreach (var item in _userMap)
            {
                if (item.Value.Observer != null)
                    notifyAction(item.Value.Observer);
            }
        }

        private static Lazy<TypeAliasTable> _typeTable = new Lazy<TypeAliasTable>(() =>
        {
            var typeTable = new TypeAliasTable();
            return typeTable;
        });

        private static Lazy<TypeModel> _typeModel = new Lazy<TypeModel>(() =>
        {
            var typeModel = TypeModel.Create();
            return typeModel;
        });

        async Task<Tuple<int, GameInfo>> IGame.Enter(string userId, IGameObserver observer)
        {
            if (_userMap.ContainsKey(userId))
                throw new InvalidOperationException();

            NotifyToAllObservers(o => o.Enter(userId));

            var clientId = ++_lastClientId;

            var userData = new UserData
            {
                UserActor = new UserRef(Sender, this, null),
                Observer = (GameObserver)observer,
                ClientId = clientId,
            };

            userData.OutboundChannel = new ProtobufChannelToClientZoneOutbound
            {
                TypeTable = _typeTable.Value,
                TypeModel = _typeModel.Value,
                OutboundChannel = userData
            };

            userData.InboundChannel = new ProtobufChannelToServerZoneInbound
            {
                TypeTable = _typeTable.Value,
                TypeModel = _typeModel.Value,
                InboundServerZone = _zone
            };

            _userMap.Add(userId, userData);
            _zone.AddClient(clientId, userData.OutboundChannel);

            // TEST SPAWN
            _zone.RunAction(zone =>
            {
                zone.Spawn(typeof(ISpaceShip), clientId, EntityFlags.Normal);
            });

            return Tuple.Create(
                clientId,
                new GameInfo
                {
                    Name = _name,
                    Users = _userMap.Keys.ToList(),
                });
        }

        async Task IGame.Leave(string userId)
        {
            UserData user;
            if (_userMap.TryGetValue(userId, out user) == false)
                throw new InvalidOperationException();

            _zone.RunAction(zone =>
            {
                zone.RemoveClient(user.ClientId);
            });
            _userMap.Remove(userId);

            NotifyToAllObservers(o => o.Leave(userId));
        }

        async Task IGameClient.ZoneChange(string senderUserId, byte[] bytes)
        {
            UserData user;
            if (_userMap.TryGetValue(senderUserId, out user) == false)
                throw new InvalidOperationException();

            _zone.RunAction(_ =>
            {
                user.InboundChannel.Write(bytes);
            });
        }
    }
}
