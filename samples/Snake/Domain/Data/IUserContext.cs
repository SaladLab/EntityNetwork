using ProtoBuf;
using TrackableData;

namespace Domain
{
    [ProtoContract]
    public interface IUserContext : ITrackableContainer<IUserContext>
    {
        [ProtoMember(1)] TrackableUserData Data { get; set; }
    }
}
