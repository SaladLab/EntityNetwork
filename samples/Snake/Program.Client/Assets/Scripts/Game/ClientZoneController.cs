using Domain;
using UnityEngine;

public class ClientZoneController : ZoneControllerClientBase, IZoneControllerClientHandler
{
    void IZoneControllerClientHandler.OnBegin()
    {
        Debug.Log("ClientZoneController.OnBegin");
    }

    void IZoneControllerClientHandler.OnEnd()
    {
        Debug.Log("ClientZoneController.OnEnd");
    }
}
