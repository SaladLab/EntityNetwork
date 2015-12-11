using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IGame : IInterfacedActor
    {
        Task<Tuple<int, GameInfo>> Enter(string userId, IGameObserver observer);
        Task Leave(string userId);
    }
}
