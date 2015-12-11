using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using Domain.Entity;
using TrackableData;
using TypeAlias;

public class TestScene : MonoBehaviour
{
    void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("Canvas").transform;

        var typeTable = new TypeAliasTable();
        EntityNetworkManager.TypeTable = typeTable;

        EntityNetworkManager.ProtobufTypeModel = new DomainProtobufSerializer();
    }
}
