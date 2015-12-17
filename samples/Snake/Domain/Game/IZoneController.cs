using EntityNetwork;
using TypeAlias;

namespace Domain
{
    [TypeAlias]
    public interface IZoneController : IEntityPrototype
    {
        [ToClient]
        void Begin();

        [ToClient]
        void End();
    }
}
