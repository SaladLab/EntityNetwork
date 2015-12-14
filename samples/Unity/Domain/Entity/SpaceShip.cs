using System;
using EntityNetwork;

namespace Domain.Entity
{
    public class ServerSpaceShip : SpaceShipServerBase, ISpaceShipServerHandler
    {
        public override SpaceShipSnapshot OnSnapshot()
        {
            return new SpaceShipSnapshot();
        }

        public void OnSay(string msg)
        {
            Console.WriteLine($"Say({msg})");
            Say(msg);
        }

        public void OnShoot(float x, float y, float dx, float dy)
        {
            Console.WriteLine($"SpaceShip({Id}).Shoot({x}, {y}, {dx}, {dy})");

            // TEST
            var bullet = (ServerBullet)Zone.Spawn(typeof(IBullet), 0, EntityFlags.Normal, null);
            bullet.Hit(1, 1);
        }
    }
}
