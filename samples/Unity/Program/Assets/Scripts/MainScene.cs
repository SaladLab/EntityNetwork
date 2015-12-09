using UnityEngine;
using System.Collections;
using Basic;
using TrackableData;
using TypeAlias;

public class MainScene : MonoBehaviour
{
    void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("Canvas").transform;

        var typeTable = new TypeAliasTable();
        typeTable.AddTypeAlias(typeof(ISpaceShip), 12);
        typeTable.AddTypeAlias(typeof(IBullet), 13);
        EntityNetworkManager.TypeTable = typeTable;

        EntityNetworkManager.ProtobufTypeModel = new EntityProtobufSerializer();
    }
}
