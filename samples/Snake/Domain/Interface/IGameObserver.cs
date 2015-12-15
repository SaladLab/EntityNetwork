using System;
using Akka.Interfaced;

namespace Domain
{
    public interface IGameObserver : IInterfacedObserver
    {
        void Join(long userId, string userName, int clientId);
        void Leave(long userId);
        void ZoneMessage(byte[] bytes);
    }
}
