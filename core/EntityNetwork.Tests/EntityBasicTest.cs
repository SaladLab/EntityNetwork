using System;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityBasicTest
    {
        [Fact]
        public void Spawn_SyncedToClient()
        {
            // Arrange

            var s = TestZoneBuilder.Build();

            // Act

            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(IBullet), 1));
            Assert.NotNull(so);
            Assert.IsType<ServerBullet>(so);

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                IClientEntity co = clientZone.GetEntity(so.Id);
                Assert.NotNull(co);
                Assert.IsType<ClientBullet>(co);
            }
        }

        [Fact]
        public void Client_CallToServer_Called()
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
        public void Server_CallToClient_Called()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.Normal));

            // Act

            s.ServerZone.RunAction(z => ((ServerSpaceShip)so).Hit(1, 2));

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                Assert.Equal(Tuple.Create(so.Id, "OnHit(1, 2)"),
                             clientZone.Log(-1));
            }
        }

        [Fact]
        public void Client_CallPassThrough_Called()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.Normal));

            // Act

            s.ClientZones[0].RunAction(z => ((ClientSpaceShip)z.GetEntity(so.Id)).Say("Hello"));

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                Assert.Equal(Tuple.Create(so.Id, "OnSay(Hello)"),
                             clientZone.Log(-1));
            }
        }

        [Fact]
        public void AddClient_AddedClientZone_Synced()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(ISpaceShip), 1, EntityFlags.Normal));

            // Act

            var zone = s.AddClient();

            // Assert

            var co = zone.Value.GetEntity(so.Id);
            Assert.NotNull(co);
            Assert.IsType<ClientSpaceShip>(co);
        }
    }
}
