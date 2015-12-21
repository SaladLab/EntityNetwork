using System;
using System.Linq;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntitySingletonTest
    {
        [Fact]
        public void Spawn_SingletonEntity_When_NoExists_Succeed()
        {
            // Arrange

            var s = TestZoneBuilder.Build();

            // Act

            IServerEntity so = null;
            s.ServerZone.RunAction(z => so = z.Spawn(typeof(IEarth), 1));

            // Assert

            Assert.NotNull(so);
            Assert.IsType<ServerEarth>(so);
        }

        [Fact]
        public void Spawn_SingletonEntity_When_AlreadyExists_Fail()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            s.ServerZone.RunAction(z => z.Spawn(typeof(IEarth), 1));

            // Act & Assert

            Assert.Throws<InvalidOperationException>(() =>
            {
                s.ServerZone.RunAction(z => z.Spawn(typeof(IEarth), 1));
            });
        }
    }
}
