using System;
using Akka.Interfaced;

namespace Domain
{
    public interface IGameObserver : IInterfacedObserver
    {
        void Enter(string userId);
        void Leave(string userId);
        void ZoneChange(byte[] bytes);
    }
}
