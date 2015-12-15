using Domain.Entity;
using UnityEngine;

public class ClientFruit : FruitClientBase, IFruitClientHandler
{
    public override void OnSnapshot(FruitSnapshot snapshot)
    {
        transform.localPosition = new Vector3(snapshot.Pos.Item1 * ClientSnake.BlockSize,
                                              snapshot.Pos.Item2 * ClientSnake.BlockSize, 0);
    }
}
