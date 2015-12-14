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

            Assert.Equal(Tuple.Create(so.Id, "InvalidOwnershipInvoke"),
                         s.ServerZone.Log(-1));
        }

        [Fact]
        public void NonOwner_CanControl_Entity_WhenEntityHasAnyoneCanControlFlag()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.AnyoneCanControl));

            // Act

            s.ClientZones[1].RunAction(z => ((ClientSpaceShip)z.GetEntity(so.Id)).Shoot(1, 2));

            // Assert

            Assert.Equal(Tuple.Create(so.Id, "OnShoot(1, 2)"),
                         s.ServerZone.Log(-1));
        }

        [Fact]
        public void NonOwner_CanCall_Entity_WhenEntityMethodHasAnyoneCanCallAttribute()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(IBullet), 1));

            // Act

            s.ClientZones[1].RunAction(z => ((ClientBullet)z.GetEntity(so.Id)).Tag("Silver"));

            // Assert

            Assert.Equal(Tuple.Create(so.Id, "OnTag(Silver)"),
                         s.ServerZone.Log(-1));
        }

        [Fact]
        public void OwnedEntity_AutoDespawn_WhenOwnerRemoved_Entity()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1));

            // Act

            s.RemoveClient(1);

            // Assert

            Assert.Null(s.ServerZone.GetEntity(so.Id));
        }

        [Fact]
        public void LiveWhenOwnerGoAwayEntity_HandOver_WhenOwnerRemoved_Entity()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.LiveWhenOwnerGoAway));

            // Act

            s.RemoveClient(1);

            // Assert

            var s2 = s.ServerZone.GetEntity(so.Id);
            Assert.NotNull(s2);
            Assert.Equal(2, s2.OwnerId);

            var c2 = s.ClientZones[0].GetEntity(so.Id);
            Assert.NotNull(c2);
            Assert.Equal(2, c2.OwnerId);
        }

        [Fact]
        public void LiveWhenOwnerGoAwayEntity_GetHandOver_WhenAllOwnerRemoved_Entity()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.LiveWhenOwnerGoAway));

            // Act

            s.RemoveClient(1);
            s.RemoveClient(2);
            s.AddClient();

            // Assert

            var s2 = s.ServerZone.GetEntity(so.Id);
            Assert.NotNull(s2);
            Assert.Equal(3, s2.OwnerId);

            var c2 = s.ClientZones[0].GetEntity(so.Id);
            Assert.NotNull(c2);
            Assert.Equal(3, c2.OwnerId);
        }

        [Fact]
        public void ChangeOwnership()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1));

            // Act

            s.ServerZone.RunAction(z => z.SetEntityOwnership(so.Id, 2));

            // Assert

            var s2 = s.ServerZone.GetEntity(so.Id);
            Assert.NotNull(s2);
            Assert.Equal(2, s2.OwnerId);

            var c2 = s.ClientZones[0].GetEntity(so.Id);
            Assert.NotNull(c2);
            Assert.Equal(2, c2.OwnerId);
        }
    }
}
