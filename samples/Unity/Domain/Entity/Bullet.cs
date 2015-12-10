using System;

namespace Domain.Entity
{
    public class ServerBullet : BulletServerBase, IBulletServerHandler
    {
        public void OnHit(float x = 0, float y = 0)
        {
            Console.WriteLine($"Bullet({Id}).Hit({x}, {y})");
        }
    }
}
