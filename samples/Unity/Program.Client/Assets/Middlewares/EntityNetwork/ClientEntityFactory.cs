using System;
using EntityNetwork;
using EntityNetwork.Unity3D;
using UnityEngine;

public class ClientEntityFactory : IClientEntityFactory
{
    private static ClientEntityFactory _default;

    public static ClientEntityFactory Default
    {
        get { return _default ?? (_default = new ClientEntityFactory()); }
    }

    public Transform RootTransform { get; set; }

    IClientEntity IClientEntityFactory.Create(Type protoTypeType)
    {
        var resource = Resources.Load("Client" + protoTypeType.Name.Substring(1));
        var go = (GameObject)GameObject.Instantiate(resource);
        if (RootTransform != null)
            go.transform.SetParent(RootTransform, false);

        return go.GetComponent<IClientEntity>();
    }

    void IClientEntityFactory.Delete(IClientEntity entity)
    {
        var enb = ((EntityNetworkBehaviour)entity);
        GameObject.Destroy(enb.gameObject);
    }
}
