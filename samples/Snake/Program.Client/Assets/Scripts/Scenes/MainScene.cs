using UnityEngine;

public class MainScene : MonoBehaviour
{
    protected void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();
    }
}
