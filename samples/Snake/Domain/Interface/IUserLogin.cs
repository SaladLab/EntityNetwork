﻿using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class LoginResult
    {
        [ProtoMember(1)] public long UserId;
        [ProtoMember(2)] public int UserActorBindId;
        [ProtoMember(3)] public TrackableUserContext UserContext;
    }

    public interface IUserLogin : IInterfacedActor
    {
        Task<LoginResult> Login(string id, string password, int observerId);
    }
}
