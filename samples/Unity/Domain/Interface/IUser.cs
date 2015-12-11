using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUser : IInterfacedActor
    {
        Task<string> GetId();
        Task<Tuple<int ,int, GameInfo>> EnterGame(string name, int observerId);
        Task LeaveGame();
    }
}
