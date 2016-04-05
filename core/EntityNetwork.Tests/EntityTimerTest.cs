using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityTimerTest
    {
        [Fact]
        private void ServerEntity_SetTimerOnce()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity o = null;
            s.ServerZone.RunAction(z => { o = z.Spawn(typeof(IBullet), 0); });

            // Act & Assert

            var called = false;
            o.SetTimerOnce(1, TimeSpan.FromSeconds(1), (e, t) =>
            {
                Assert.Equal(e, o);
                Assert.Equal(1, t);
                called = true;
            });
            s.UpdateTime(TimeSpan.FromSeconds(1));
            Assert.True(called);
        }

        [Fact]
        private void ServerEntity_SetTimerRepeatedly()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity o = null;
            s.ServerZone.RunAction(z => { o = z.Spawn(typeof(IBullet), 0); });

            // Act & Assert

            var callCount = 0;
            o.SetTimerRepeatedly(1, TimeSpan.FromSeconds(1), (e, t) =>
            {
                Assert.Equal(e, o);
                Assert.Equal(1, t);
                callCount += 1;
            });

            s.UpdateTime(TimeSpan.FromSeconds(1));
            Assert.Equal(1, callCount);

            s.UpdateTime(TimeSpan.FromSeconds(1));
            Assert.Equal(2, callCount);
        }

        [Fact]
        private void ServerEntity_RemoveTimer()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity o = null;
            s.ServerZone.RunAction(z => { o = z.Spawn(typeof(IBullet), 0); });

            // Act & Assert

            var called1 = false;
            var called2 = false;
            o.SetTimerOnce(1, TimeSpan.FromSeconds(1), (e, t) =>
            {
                called1 = true;
            });
            o.SetTimerOnce(2, TimeSpan.FromSeconds(1), (e, t) =>
            {
                called2 = true;
            });

            o.RemoveTimer(1);
            s.UpdateTime(TimeSpan.FromSeconds(1));

            Assert.False(called1);
            Assert.True(called2);
        }

        [Fact]
        private void ServerEntity_RemoveTimerAll()
        {
            // Arrange

            var s = TestZoneBuilder.Build();
            IServerEntity o = null;
            s.ServerZone.RunAction(z => { o = z.Spawn(typeof(IBullet), 0); });

            // Act & Assert

            var called1 = false;
            var called2 = false;
            o.SetTimerOnce(1, TimeSpan.FromSeconds(1), (e, t) =>
            {
                called1 = true;
            });
            o.SetTimerOnce(2, TimeSpan.FromSeconds(1), (e, t) =>
            {
                called2 = true;
            });

            o.RemoveTimerAll();
            s.UpdateTime(TimeSpan.FromSeconds(1));

            Assert.False(called1);
            Assert.False(called2);
        }
    }
}
