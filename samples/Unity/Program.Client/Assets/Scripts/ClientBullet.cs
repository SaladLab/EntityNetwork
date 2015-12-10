using Domain.Entity;
using UnityEngine;

public class ClientBullet : BulletClientBase, IBulletClientHandler
{
    void IBulletClientHandler.OnHit(float x, float y)
    {
        Debug.Log("OnHit");
    }
}
