using System;
using EntityNetwork;

namespace Basic
{
    public class ServerSpaceShip : SpaceShipServerBase, ISpaceShipServerHandler
    {
        public override SpaceShipSnapshot OnSnapshot()
        {
            return new SpaceShipSnapshot { Name = "Houston" };
        }

        public void OnSay(string msg)
        {
            Console.WriteLine($"Say({msg})");
            Say(msg);
            Data.Score += 1;
        }

        public void OnShoot(float x, float y, float dx, float dy)
        {
            Console.WriteLine($"SpaceShip({Id}).Shoot({x}, {y}, {dx}, {dy})");

            // TEST
            var bullet = (ServerBullet)Zone.Spawn(typeof(IBullet), 0, EntityFlags.Normal);
            bullet.Hit(1, 1);
        }
    }

    public class ClientSpaceShip : SpaceShipClientBase, ISpaceShipClientHandler
    {
        public override void OnSnapshot(SpaceShipSnapshot snapshot)
        {
            Console.WriteLine($"Client.OnSnapshot({snapshot.Name})");
        }

        public void OnSay(string msg)
        {
            Console.WriteLine($"Client.OnSay({msg})");
        }

        public void OnMove(float x, float y, float dx, float dy)
        {
            Console.WriteLine($"Client.OnMove({x}, {y}, {dx}, {dy})");
        }

        public void OnStop(float x, float y)
        {
            Console.WriteLine($"Client.OnStop({x}, {y})");
        }

        public void OnHit(float x = 0, float y = 0)
        {
            Console.WriteLine($"Client.OnHit({x}, {y})");
        }
    }
}
