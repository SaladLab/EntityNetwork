using System;
using ProtoBuf;
using System.Collections.Generic;

namespace Domain
{
    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        Ended,
        Aborted,
    }

    public enum GameResult
    {
        None,
        Win,
        Lose,
        Draw,
    }

    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public long Id;
        [ProtoMember(2)] public GameState State;
        [ProtoMember(3)] public List<string> PlayerNames;
    }
}
