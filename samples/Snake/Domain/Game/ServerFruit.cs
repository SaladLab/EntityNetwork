using System;
using System.Linq;
using EntityNetwork;

namespace Domain
{
    public class ServerFruit : FruitServerBase, IFruitServerHandler
    {
        public Tuple<int, int> Pos { get; private set; }

        public override void OnSpawn(object param)
        {
            Pos = (Tuple<int, int>)param;
        }

        public override FruitSnapshot OnSnapshot()
        {
            return new FruitSnapshot { Pos = Pos };
        }

        public static ServerFruit Spawn(ServerZone zone)
        {
            var rnd = new Random();

            var snakes = zone.GetEntities(typeof(ISnake)).Select(e => (ServerSnake)e).ToArray();
            while (true)
            {
                var x = rnd.Next(Rule.BoardWidth);
                var y = rnd.Next(Rule.BoardHeight);

                if (snakes.All(s => s.Parts.All(p => p.Item1 != x || p.Item2 != y)))
                {
                    var so = zone.Spawn(typeof(IFruit), 0, EntityFlags.Normal, Tuple.Create(x, y));
                    return (ServerFruit)so;
                }
            }
        }
    }
}
