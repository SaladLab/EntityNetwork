using Domain.Entity;
using EntityNetwork;
using UnityEngine;

public class ClientSpaceShip : SpaceShipClientBase, ISpaceShipClientHandler
{
    private Vector2 _moveDirection = Vector2.zero;
    private float _moveSpeed = 100;

    private void FixedUpdate()
    {
        if (OwnerId == Zone.ClientId)
            UpdateInput();
    }

    private void Update()
    {
        // update position

        if (_moveDirection != Vector2.zero)
        {
            var rt = GetComponent<RectTransform>();
            var pos = rt.localPosition +
                      new Vector3(_moveDirection.x, _moveDirection.y, 0) * _moveSpeed * Time.deltaTime;
            rt.localPosition = pos;
        }
    }

    private void UpdateInput()
    {
        var dir = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow))
            dir.x = -1;
        if (Input.GetKey(KeyCode.RightArrow))
            dir.x = +1;
        if (Input.GetKey(KeyCode.UpArrow))
            dir.y = +1;
        if (Input.GetKey(KeyCode.DownArrow))
            dir.y = -1;

        if (dir != _moveDirection)
        {
            var rt = GetComponent<RectTransform>();
            ((ClientZone)Zone).RunAction(_ => { Move(rt.localPosition.x, rt.localPosition.y, dir.x, dir.y); });
            _moveDirection = dir;
        }
    }

    public override void OnSnapshot(SpaceShipSnapshot snapshot)
    {
    }

    void ISpaceShipClientHandler.OnSay(string msg)
    {
        Debug.Log("OnSay");
    }

    void ISpaceShipClientHandler.OnMove(float x, float y, float dx, float dy)
    {
        if (OwnerId == Zone.ClientId)
            return;

        var rt = GetComponent<RectTransform>();
        rt.localPosition = new Vector3(x, y, 0);
        _moveDirection = new Vector2(dx, dy);
    }

    void ISpaceShipClientHandler.OnStop(float x, float y)
    {
        Debug.Log("OnStop");
    }

    void ISpaceShipClientHandler.OnHit(float x, float y)
    {
        Debug.Log("OnHit");
    }
}
