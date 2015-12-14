using System;
using System.Linq;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityDataTest
    {
        [Fact]
        public void Spawn_Snapshot_Synced()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;

            // Act

            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.Normal, "Enterprise"));

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                Assert.Equal(Tuple.Create(so.Id, "OnSnapshot(Enterprise)"),
                             clientZone.Log(-2));
                Assert.Equal(Tuple.Create(so.Id, "Spawn"),
                             clientZone.Log(-1));
            }
        }

        [Fact]
        public void Change_TrackableData_Synced()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.Normal));

            // Act

            s.ClientZones[0].RunAction(z => ((ClientSpaceShip)z.GetEntity(so.Id)).Shoot(1, 2));

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                var clientEntity = (ClientSpaceShip)clientZone.GetEntity(so.Id);
                Assert.Equal(1, clientEntity.Data.Score);
            }
        }
    }
}
