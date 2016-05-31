using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUser : IInterfacedActor
    {
        Task<string> GetId();
        Task<Tuple<IGameClient, int, GameInfo>> EnterGame(string name, IGameObserver observer);
        Task LeaveGame();
    }
}
