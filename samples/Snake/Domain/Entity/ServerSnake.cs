using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Game;
using EntityNetwork;

namespace Domain.Entity
{
    public class ServerSnake : SnakeServerBase, ISnakeServerHandler
    {
        public List<Tuple<int, int>> Parts { get; private set; }

        public override void OnSpawn(object param)
        {
            Parts = ((SnakeSnapshot)param).Parts;

            Data.Score = 0;
            Data.State = SnakeState.Ready;
        }

        public override SnakeSnapshot OnSnapshot()
        {
            return new SnakeSnapshot { Parts = Parts };
        }

        public void OnDebugGrowUp(int length)
        {
            for (var i=0; i<length ;i++)
                Parts.Add(Parts.Last());

            GrowUp(length);
        }

        public void OnMove(int x, int y)
        {
            // Move parts

            for (int i = Parts.Count - 1; i >= 1; i--)
            {
                Parts[i] = Parts[i - 1];
            }
            Parts[0] = Tuple.Create(x, y);

            // Check hit wall

            if (x < 0 || x >= Rule.BoardWidth)
            {
                Data.State = SnakeState.Dead;
                return;
            }
            if (y < 0 || y >= Rule.BoardHeight)
            {
                Data.State = SnakeState.Dead;
                return;
            }

            // Check hit parts of snakes

            foreach (var entity in Zone.GetEntities(typeof(ISnake)))
            {
                var snake = (ServerSnake)entity;
                var hit = snake.Parts.Skip(snake == this ? 1 : 0).Any(p => p.Item1 == x && p.Item2 == y);
                if (hit)
                {
                    Data.State = SnakeState.Dead;
                    return;
                }
            }

            // Check hit fruits

            ServerFruit hitFruit = null;
            foreach (var entity in Zone.GetEntities(typeof(IFruit)))
            {
                var fruit = (ServerFruit)entity;
                if (fruit.Pos.Item1 == x && fruit.Pos.Item2 == y)
                {
                    hitFruit = fruit;
                    break;
                }
            }
            if (hitFruit != null)
            {
                Zone.Despawn(hitFruit.Id);
                Data.Score += 1;
                GrowUp(1);
                ServerFruit.Spawn((ServerZone)Zone);
            }

            Move(x, y);
        }
    }
}
