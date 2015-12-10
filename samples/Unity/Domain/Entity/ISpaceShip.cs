using EntityNetwork;

namespace Domain.Entity
{
    public interface ISpaceShip : IEntityPrototype
    {
        void Say(string msg);
        [PassThrough] void Move(float x, float y, float dx, float dy);
        [PassThrough] void Stop(float x, float y);
        [ToServer] void Shoot(float x, float y, float dx, float dy);
        [ToClient] void Hit(float x = 0f, float y = 0f);
    }
}
