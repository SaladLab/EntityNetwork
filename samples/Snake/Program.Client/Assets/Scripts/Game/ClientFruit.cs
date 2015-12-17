using Domain;
using UnityEngine;

public class ClientFruit : FruitClientBase, IFruitClientHandler
{
    public override void OnSnapshot(FruitSnapshot snapshot)
    {
        GetComponent<RectTransform>().anchoredPosition = new Vector2(
            snapshot.Pos.Item1 * ClientSnake.BlockSize,
            snapshot.Pos.Item2 * ClientSnake.BlockSize);
    }
}
