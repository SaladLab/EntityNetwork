using System;
using ProtoBuf;
using System.Collections.Generic;

namespace Domain
{
    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public List<string> Users;
    }
}
