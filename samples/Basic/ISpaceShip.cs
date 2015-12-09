using EntityNetwork;
using ProtoBuf;
using TrackableData;
using TypeAlias;

namespace Basic
{
    [TypeAlias]
    public interface ISpaceShip : IEntityPrototype
    {
        SpaceShipSnapshot Snapshot { get; }
        ISpaceShipData Data { get; }

        void Say(string msg);
        [PassThrough] void Move(float x, float y, float dx, float dy);
        [PassThrough] void Stop(float x, float y);
        [ToServer] void Shoot(float x, float y, float dx, float dy);
        [ToClient] void Hit(float x = 0f, float y = 0f);
    }

    [ProtoContract, TypeAlias]
    public class SpaceShipSnapshot
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public float X;
        [ProtoMember(3)] public float Y;
    }

    [ProtoContract]
    public interface ISpaceShipData : ITrackablePoco<ISpaceShipData>
    {
        [ProtoMember(1)] float Hp { get; set; }
        [ProtoMember(2)] float Score { get; set; }
    }
}
