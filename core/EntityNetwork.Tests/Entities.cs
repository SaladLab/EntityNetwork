using System;
using System.Collections.Generic;
using ProtoBuf;
using TrackableData;
using TypeAlias;

namespace EntityNetwork.Tests
{
    [TypeAlias]
    public interface IBullet : IEntityPrototype
    {
        void Hit(int x, int y);

        [ToServer, AnyoneCanCall]
        void Tag(string tag);
    }

    public class ServerBullet : BulletServerBase, IBulletServerHandler
    {
        private Tuple<int, int> _pos;

        public override void OnSpawn(object param)
        {
            var pos = (Tuple<int, int>)param;
            _pos = pos;
        }

        public void OnHit(int x = 0, int y = 0)
        {
            Log($"OnHit({x}, {y})");
        }

        public void OnTag(string tag)
        {
            Log($"OnTag({tag})");
        }
    }

    public class ClientBullet : BulletClientBase, IBulletClientHandler
    {
        public void OnHit(int x = 0, int y = 0)
        {
            Log($"OnHit({x}, {y})");
        }
    }

    [TypeAlias]
    public interface ISpaceShip : IEntityPrototype
    {
        ISpaceShipData Data { get; }
        SpaceShipSnapshot Snapshot { get; }

        [PassThrough]
        void Say(string msg);

        void Move(int x, int y);

        [ToServer]
        void Shoot(int x, int y);

        [ToClient]
        void Hit(int x, int y);
    }

    [ProtoContract]
    public interface ISpaceShipData : ITrackablePoco<ISpaceShipData>
    {
        [ProtoMember(1)] int Hp { get; set; }
        [ProtoMember(2)] int Score { get; set; }
    }

    [ProtoContract, TypeAlias]
    public class SpaceShipSnapshot
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public int X;
        [ProtoMember(3)] public int Y;
    }

    public class ServerSpaceShip : SpaceShipServerBase, ISpaceShipServerHandler
    {
        private string _name;
        private Tuple<int, int> _pos;

        public override void OnSpawn(object param)
        {
            _name = (string)param;
            _pos = Tuple.Create(0, 0);
        }

        public override SpaceShipSnapshot OnSnapshot()
        {
            return new SpaceShipSnapshot { Name = _name, X = _pos.Item1, Y = _pos.Item2 };
        }

        public void OnMove(int x, int y)
        {
            Log($"OnMove({x}, {y})");
            _pos = Tuple.Create(x, y);
        }

        public void OnShoot(int x, int y)
        {
            Log($"OnShoot({x}, {y})");
            Data.Score += 1;
        }
    }

    public class ClientSpaceShip : SpaceShipClientBase, ISpaceShipClientHandler
    {
        private string _name;
        private Tuple<int, int> _pos;

        public override void OnSnapshot(SpaceShipSnapshot snapshot)
        {
            Log($"OnSnapshot({snapshot.Name})");
            _name = snapshot.Name;
            _pos = Tuple.Create(snapshot.X, snapshot.Y);
        }

        public void OnSay(string msg)
        {
            Log($"OnSay({msg})");
        }

        public void OnMove(int x, int y)
        {
            Log($"OnMove({x}, {y})");
            _pos = Tuple.Create(x, y);
        }

        public void OnHit(int x = 0, int y = 0)
        {
            Log($"OnHit({x}, {y})");
        }
    }

    [TypeAlias, Singleton]
    public interface IEarth : IEntityPrototype
    {
    }

    public class ServerEarth : EarthServerBase, IEarthServerHandler
    {
    }

    public class ClientEarth : EarthClientBase, IEarthClientHandler
    {
    }

    [TypeAlias, ServerOnly]
    public interface IMonitor : IEntityPrototype
    {
    }

    public class ServerMonitor : MonitorServerBase, IMonitorServerHandler
    {
    }
}
