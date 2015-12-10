using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    // Any user who is in a game
    [TagOverridable("senderUserId")]
    public interface IGameClient : IInterfacedActor
    {
        Task ZoneChange(byte[] bytes);
    }
}
