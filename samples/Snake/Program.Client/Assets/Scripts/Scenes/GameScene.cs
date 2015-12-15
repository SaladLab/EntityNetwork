using UnityEngine;

public class GameScene : MonoBehaviour
{
    protected void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();

        // UiMessageBox.ShowMessageBox("Test");
    }
}
