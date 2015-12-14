using System;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityOwnershipTest
    {
        [Fact]
        public void Owner_CanControl_Entity()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1));

            // Act

            s.ClientZones[0].RunAction(z => ((ClientSpaceShip)z.GetEntity(so.Id)).Shoot(1, 2));

            // Assert

            Assert.Equal(Tuple.Create(so.Id, "OnShoot(1, 2)"),
                         s.ServerZone.Log(-1));
        }

        [Fact]
        public void NonOwner_CannotControl_Entity()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1));

            // Act

            s.ClientZones[1].RunAction(z => ((ClientSpaceShip)z.GetEntity(so.Id)).Shoot(1, 2));

            // Assert

            Assert.Equal(Tuple.Create(so.Id, "OnShoot(1, 2)"),
                         s.ServerZone.Log(-1));
        }

        [Fact]
        public void OwnedEntity_AutoDespawn_WhenOwnerRemoved_Entity()
        {
        }

        [Fact]
        public void LiveWhenOwnerGoAwayEntity_HandOver_WhenOwnerRemoved_Entity()
        {
        }
    }
}
