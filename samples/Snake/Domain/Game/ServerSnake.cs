﻿using System;
using System.Collections.Generic;
using System.Linq;
using EntityNetwork;

namespace Domain
{
    public class ServerSnake : SnakeServerBase, ISnakeServerHandler
    {
        public List<Tuple<int, int>> Parts { get; private set; }
        public bool UseAi { get; private set; }

        public override void OnSpawn(object param)
        {
            var snapshot = (SnakeSnapshot)param;

            Parts = snapshot.Parts;
            UseAi = snapshot.UseAi;

            Data.Score = 0;
            Data.State = SnakeState.Ready;
        }

        public override SnakeSnapshot OnSnapshot()
        {
            return new SnakeSnapshot { Parts = Parts, UseAi = UseAi };
        }

        void ISnakeServerHandler.OnMove(int x, int y)
        {
            // Move parts

            for (int i = Parts.Count - 1; i >= 1; i--)
            {
                Parts[i] = Parts[i - 1];
            }
            Parts[0] = Tuple.Create(x, y);

            // Check hit wall

            if (x < 0 || x >= Rule.BoardWidth || y < 0 || y >= Rule.BoardHeight)
            {
                MakeDead();
                return;
            }

            // Check hit parts of snakes

            foreach (var snake in Zone.GetEntities<ServerSnake>())
            {
                var hit = snake.Parts.Skip(snake == this ? 1 : 0).Any(p => p.Item1 == x && p.Item2 == y);
                if (hit)
                {
                    MakeDead();
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
                Parts.Add(Parts.Last());
                GrowUp(1);
            }

            Move(x, y);
        }

        public void MakePlaying()
        {
            Data.State = SnakeState.Playing;
        }

        public void MakeDead()
        {
            Data.State = SnakeState.Stopped;
            Zone.GetEntity<ServerZoneController>().OnSnakeDead(this);
        }
    }
}
