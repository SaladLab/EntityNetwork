using System;
using Xunit;

namespace EntityNetwork.Tests
{
    public class EntityFactoryTest
    {
        [Fact]
        public void ServerEntityFactory_GetProtoType_From_ServerEntityType()
        {
            var factory = (IServerEntityFactory)new EntityFactory();
            var entityType = factory.GetProtoType(typeof(BulletServerBase));
            Assert.Equal(typeof(IBullet), entityType);
        }

        [Fact]
        public void ServerEntityFactory_GetProtoType_From_WrongEntityType()
        {
            var factory = (IServerEntityFactory)new EntityFactory();
            var entityType = factory.GetProtoType(typeof(BulletClientBase));
            Assert.Equal(null, entityType);
        }

        [Fact]
        public void ClientEntityFactory_GetProtoType_From_ClientEntityType()
        {
            var factory = (IClientEntityFactory)new EntityFactory();
            var entityType = factory.GetProtoType(typeof(BulletClientBase));
            Assert.Equal(typeof(IBullet), entityType);
        }

        [Fact]
        public void ClientEntityFactory_GetProtoType_From_WrongEntityType()
        {
            var factory = (IClientEntityFactory)new EntityFactory();
            var entityType = factory.GetProtoType(typeof(BulletServerBase));
            Assert.Equal(null, entityType);
        }
    }
}
