using UnityEngine;

public class TestScene : MonoBehaviour
{
    private void Start()
    {
        ClientEntityFactory.Default.RootTransform = GameObject.Find("Entities").transform;
    }
}
