using TypeAlias;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    protected void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("GameEntityRoot").transform;

        var typeTable = new TypeAliasTable();
        EntityNetworkManager.TypeTable = typeTable;
        EntityNetworkManager.ProtobufTypeModel = new DomainProtobufSerializer();

        ApplicationComponent.TryInit();
        UiManager.Initialize();

        // UiMessageBox.ShowMessageBox("Test");
    }
}
