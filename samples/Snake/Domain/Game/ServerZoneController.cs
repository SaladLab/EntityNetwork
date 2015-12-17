using System;
using System.Collections.Generic;
using System.Linq;
using EntityNetwork;

namespace Domain
{
    public class ServerZoneController : ZoneControllerServerBase, IZoneControllerServerHandler
    {
        public Action<bool> StateChanged;

        public void Start(int clientId1, int clientId2)
        {
            SpawnSnakes(clientId1, clientId2);
            SpawnFruit();
        }

        private void SpawnSnakes(int clientId1, int clientId2)
        {
            var x1 = Rule.BoardWidth / 2;
            var x2 = Rule.BoardWidth / 2 + 1;
            var y1 = Rule.BoardHeight / 4;
            var y2 = Rule.BoardHeight * 3 / 4;

            Zone.Spawn(typeof(ISnake), clientId1, EntityFlags.Normal,
                       new SnakeSnapshot
                       {
                           Parts = new List<Tuple<int, int>>
                           {
                                   Tuple.Create(x1, y1),
                                   Tuple.Create(x2, y1)
                           }
                       });

            Zone.Spawn(typeof(ISnake), clientId2, EntityFlags.Normal,
                       new SnakeSnapshot
                       {
                           Parts = new List<Tuple<int, int>>
                           {
                                   Tuple.Create(x2, y2),
                                   Tuple.Create(x1, y2)
                           },
                           UseAi = clientId1 == clientId2,
                       });
        }

        public void OnSnakeDead(ServerSnake snake)
        {
            // TODO: GAME OVER
        }

        private void SpawnFruit()
        {
            var rnd = new Random();

            var snakes = Zone.GetEntities(typeof(ISnake)).Select(e => (ServerSnake)e).ToArray();
            while (true)
            {
                var x = rnd.Next(Rule.BoardWidth);
                var y = rnd.Next(Rule.BoardHeight);

                if (snakes.All(s => s.Parts.All(p => p.Item1 != x || p.Item2 != y)))
                {
                    Zone.Spawn(typeof(IFruit), 0, EntityFlags.Normal, Tuple.Create(x, y));
                    return;
                }
            }
        }

        public void OnFruitDespawn(ServerFruit fruit)
        {
            SpawnFruit();
            // TODO: USE TIMER
        }
    }
}
