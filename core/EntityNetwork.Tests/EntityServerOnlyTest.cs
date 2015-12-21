using System;
using System.Linq;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityServerOnlyTest
    {
        [Fact]
        public void Spawn_ServerOnlyEntity_WhenSpawn_ClientCannotSee()
        {
            // Arrange

            var s = TestZoneBuilder.Build();

            // Act

            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(IMonitor), 0));
            Assert.NotNull(so);
            Assert.IsType<ServerMonitor>(so);

            // Assert

            foreach (var clientZone in s.ClientZones)
            {
                IClientEntity co = clientZone.GetEntity(so.Id);
                Assert.Null(co);
            }
        }

        [Fact]
        public void Spawn_ServerOnlyEntity_WhenAddNewClient_ClientCannotSee()
        {
            // Arrange

            var s = TestZoneBuilder.Build();

            // Act

            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(IMonitor), 0));
            var zone = s.AddClient();

            // Assert

            var co = zone.Value.GetEntity(so.Id);
            Assert.Null(co);
        }
    }
}
