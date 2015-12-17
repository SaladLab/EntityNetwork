using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Domain;
using EntityNetwork;

public class ClientSnake : SnakeClientBase, ISnakeClientHandler
{
    public static int BlockSize = 30;

    public GameObject BlockTemplate;

    private class Part
    {
        public RectTransform Block;
        public int X;
        public int Y;
    }

    private readonly List<Part> _parts = new List<Part>();
    private bool _useAi;
    private int _posX;
    private int _posY;
    private int _orientX;
    private int _orientY;
    private float _moveTime;
    private readonly Queue<Tuple<int, int>> _inputQueue = new Queue<Tuple<int, int>>();

    public bool IsControllable { get { return OwnerId == Zone.ClientId && !_useAi; } }

    public override void OnSnapshot(SnakeSnapshot snapshot)
    {
        _parts.Clear();

        foreach (var pos in snapshot.Parts)
        {
            var part = new Part
            {
                Block = UiHelper.AddChild(gameObject, BlockTemplate).GetComponent<RectTransform>(),
                X = pos.Item1,
                Y = pos.Item2,
            };
            part.Block.gameObject.SetActive(true);
            if (_parts.Count > 0)
                part.Block.GetComponent<Image>().color = Color.gray;
            _parts.Add(part);
        }

        var e = _parts.Count - 1;
        _posX = _parts[e].X;
        _posY = _parts[e].Y;
        _orientX = _parts[e].X - _parts[e - 1].X;
        _orientY = _parts[e].Y - _parts[e - 1].Y;

        _useAi = snapshot.UseAi;
    }

    public void OnMove(int x, int y)
    {
        if (Zone.ClientId == OwnerId)
            return;

        _posX = x;
        _posY = y;
        MoveParts();
    }

    private void MoveParts()
    {
        for (int i = _parts.Count - 1; i >= 0; i--)
        {
            if (i > 0)
            {
                _parts[i].X = _parts[i - 1].X;
                _parts[i].Y = _parts[i - 1].Y;
            }
            else
            {
                _parts[i].X = _posX;
                _parts[i].Y = _posY;
            }
            _parts[i].Block.localPosition = new Vector3(_parts[i].X * BlockSize, _parts[i].Y * BlockSize, 0);
        }
    }

    public void OnGrowUp(int length)
    {
        for (int i = 0; i < length; i++)
        {
            var part = new Part
            {
                Block = UiHelper.AddChild(gameObject, BlockTemplate).GetComponent<RectTransform>(),
                X = _parts[_parts.Count - 1].X,
                Y = _parts[_parts.Count - 1].Y,
            };
            _parts.Add(part);
            part.Block.GetComponent<Image>().color = Color.gray;
            part.Block.localPosition = new Vector3(part.X * BlockSize, part.Y * BlockSize, 0);
            part.Block.gameObject.SetActive(true);
        }
    }

    protected void Update()
    {
        if (OwnerId != Zone.ClientId)
            return;

        if (Data.State != SnakeState.Dead)
        {
            _moveTime -= Time.deltaTime;
            if (_moveTime < 0)
            {
                if (_inputQueue.Count > 0)
                {
                    var orient = _inputQueue.Dequeue();
                    _orientX = orient.Item1;
                    _orientY = orient.Item2;
                }
                else if (_useAi)
                {
                    var orient = SnakeAi.Think(_posX, _posY, _orientX, _orientY);
                    _orientX = orient.Item1;
                    _orientY = orient.Item2;
                }

                _posX += _orientX;
                _posY += _orientY;

                ((ClientZone)Zone).RunAction(z => Move(_posX, _posY));

                // TODO: If it hit the wall we need to stop here ?.
                MoveParts();

                _moveTime += (float)Rule.SnakeSpeed.TotalSeconds;
            }
        }
    }

    public void QueueInput(int orientX, int orientY)
    {
        _inputQueue.Enqueue(Tuple.Create(orientX, orientY));
    }
}
