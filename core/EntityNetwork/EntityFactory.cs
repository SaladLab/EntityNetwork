using System;
using System.Collections.Generic;
using TypeAlias;

namespace EntityNetwork
{
    public interface IServerEntityFactory
    {
        IServerEntity Create(Type protoTypeType);
        void Delete(IServerEntity entity);
    }

    public interface IClientEntityFactory
    {
        IClientEntity Create(Type protoTypeType);
        void Delete(IClientEntity entity);
    }

    public class EntityFactory : IServerEntityFactory, IClientEntityFactory
    {
        private static EntityFactory _default;

        public static EntityFactory Default
        {
            get { return _default ?? (_default = new EntityFactory()); }
        }

        private Dictionary<Type, Tuple<Type, Type>> _entityTypeMap = new Dictionary<Type, Tuple<Type, Type>>();

        public EntityFactory()
        {
            BuildTypeMap();
        }

        private void BuildTypeMap()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var item in TypeUtility.GetTypesContainingAttribute<PayloadTableForEntity>(assembly))
                {
                    var type = item.Attribute.Type;
                    var typePrefix = type.Namespace.Length > 0 ? type.Namespace + "." : "";
                    var serverEntityType = type.Assembly.GetType(typePrefix + "Server" + type.Name.Substring(1)) ??
                                           TypeUtility.GetType(typePrefix + "Server" + type.Name.Substring(1));
                    var clientEntityType = type.Assembly.GetType(typePrefix + "Client" + type.Name.Substring(1)) ??
                                           TypeUtility.GetType(typePrefix + "Client" + type.Name.Substring(1));
                    _entityTypeMap.Add(type, Tuple.Create(serverEntityType, clientEntityType));
                }
            }
        }

        public Type GetServerEntityType(Type protoTypeType)
        {
            Tuple<Type, Type> entityTypes;
            return _entityTypeMap.TryGetValue(protoTypeType, out entityTypes) ? entityTypes.Item1 : null;
        }

        public Type GetClientEntityType(Type protoTypeType)
        {
            Tuple<Type, Type> entityTypes;
            return _entityTypeMap.TryGetValue(protoTypeType, out entityTypes) ? entityTypes.Item2 : null;
        }

        IServerEntity IServerEntityFactory.Create(Type protoTypeType)
        {
            var type = GetServerEntityType(protoTypeType);
            return type != null ? (IServerEntity)Activator.CreateInstance(type) : null;
        }

        void IServerEntityFactory.Delete(IServerEntity entity)
        {
        }

        IClientEntity IClientEntityFactory.Create(Type protoTypeType)
        {
            var type = GetClientEntityType(protoTypeType);
            return type != null ? (IClientEntity)Activator.CreateInstance(type) : null;
        }

        void IClientEntityFactory.Delete(IClientEntity entity)
        {
        }
    }
}
