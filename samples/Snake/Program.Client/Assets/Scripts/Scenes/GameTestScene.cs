using System.Linq;
using TypeAlias;
using UnityEngine;

public class GameTestScene : MonoBehaviour
{
    public SnakeControlPad SnakeControlPad;
    public Transform GameEntityRoot;

    protected void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameEntityRoot;

        var typeTable = new TypeAliasTable();
        EntityNetworkManager.TypeTable = typeTable;
        EntityNetworkManager.ProtobufTypeModel = new DomainProtobufSerializer();

        ApplicationComponent.TryInit();
        UiManager.Initialize();
    }

    protected void Update()
    {
        ClientSnake snake = null;

        if (EntityNetworkClient.LocalClientZone != null)
        {
            snake = EntityNetworkClient.LocalClientZone.GetEntities<ClientSnake>().FirstOrDefault(s => s.IsControllable);
        }

        SnakeControlPad.Snake = snake;
    }
}
