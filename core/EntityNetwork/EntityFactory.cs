using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TypeAlias;

namespace EntityNetwork
{
    public interface IServerEntityFactory
    {
        Type GetProtoType(Type entityType);
        IServerEntity Create(Type protoType);
        void Delete(IServerEntity entity);
    }

    public interface IClientEntityFactory
    {
        Type GetProtoType(Type entityType);
        IClientEntity Create(Type protoType);
        void Delete(IClientEntity entity);
    }

    public class EntityFactory : IServerEntityFactory, IClientEntityFactory
    {
        private static EntityFactory _default;

        public static EntityFactory Default
        {
            get { return _default ?? (_default = new EntityFactory()); }
        }

        private readonly Dictionary<Type, Tuple<Type, Type>> _entityTypeMap =
            new Dictionary<Type, Tuple<Type, Type>>();

        private readonly ConcurrentDictionary<Type, Type> _serverEntityToProtoTypeMap = 
            new ConcurrentDictionary<Type, Type>();

        private readonly ConcurrentDictionary<Type, Type> _clientEntityToProtoTypeMap =
            new ConcurrentDictionary<Type, Type>();

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

        public Type GetServerEntityType(Type protoType)
        {
            Tuple<Type, Type> entityTypes;
            return _entityTypeMap.TryGetValue(protoType, out entityTypes) ? entityTypes.Item1 : null;
        }

        public Type GetClientEntityType(Type protoType)
        {
            Tuple<Type, Type> entityTypes;
            return _entityTypeMap.TryGetValue(protoType, out entityTypes) ? entityTypes.Item2 : null;
        }

        Type IServerEntityFactory.GetProtoType(Type entityType)
        {
            return _serverEntityToProtoTypeMap.GetOrAdd(entityType, t =>
            {
                var type = entityType;
                while (type != null && type != typeof(object))
                {
                    if (type.Name.EndsWith("ServerBase"))
                    {
                        var typePrefix = type.Namespace.Length > 0 ? type.Namespace + "." : "";
                        var protoType = type.Assembly.GetType(typePrefix + "I" +
                                                              type.Name.Substring(0, type.Name.Length - 10));
                        if (protoType != null && typeof(IEntityPrototype).IsAssignableFrom(protoType))
                        {
                            return protoType;
                        }
                    }
                    type = type.BaseType;
                }
                return null;
            });
        }

        IServerEntity IServerEntityFactory.Create(Type protoType)
        {
            var type = GetServerEntityType(protoType);
            return type != null ? (IServerEntity)Activator.CreateInstance(type) : null;
        }

        void IServerEntityFactory.Delete(IServerEntity entity)
        {
        }

        Type IClientEntityFactory.GetProtoType(Type entityType)
        {
            return _clientEntityToProtoTypeMap.GetOrAdd(entityType, t =>
            {
                var type = entityType;
                while (type != null && type != typeof(object))
                {
                    if (type.Name.EndsWith("ClientBase"))
                    {
                        var typePrefix = type.Namespace.Length > 0 ? type.Namespace + "." : "";
                        var protoType = type.Assembly.GetType(typePrefix + "I" +
                                                              type.Name.Substring(0, type.Name.Length - 10));
                        if (protoType != null && typeof(IEntityPrototype).IsAssignableFrom(protoType))
                        {
                            return protoType;
                        }
                    }
                    type = type.BaseType;
                }
                return null;
            });
        }

        IClientEntity IClientEntityFactory.Create(Type protoType)
        {
            var type = GetClientEntityType(protoType);
            return type != null ? (IClientEntity)Activator.CreateInstance(type) : null;
        }

        void IClientEntityFactory.Delete(IClientEntity entity)
        {
        }
    }
}
