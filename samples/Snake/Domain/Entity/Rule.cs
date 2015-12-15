using System;

namespace Domain.Game
{    
    public static class Rule
    {
        public static readonly int BoardWidth = 23;
        public static readonly int BoardHeight = 30;
        public static readonly TimeSpan SnakeSpeed = TimeSpan.FromSeconds(0.2);
    }
}
