using System;

namespace EntityNetwork.Tests
{
    public abstract class TestServerEntity : ServerEntity
    {
        protected void Log(string msg)
        {
            var z = (TestServerZone)Zone;
            z.Logs.Add(Tuple.Create(Id, msg));
        }
    }

    public abstract class TestClientEntity : ClientEntity
    {
        protected void Log(string msg)
        {
            var z = (TestClientZone)Zone;
            z.Logs.Add(Tuple.Create(Id, msg));
        }
    }
}
