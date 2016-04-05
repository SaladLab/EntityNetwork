using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public List<string> Users;
    }
}
