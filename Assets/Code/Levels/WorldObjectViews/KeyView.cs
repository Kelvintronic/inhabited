using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class KeyView : MonoBehaviour, IObjectView, IInventoryItem
{
    private SpriteRenderer _renderer;
    private WorldObject _worldObject;

    string IInventoryItem.Name => "Key";
    Sprite IInventoryItem.Image => _renderer.sprite;
    public static KeyView Create(KeyView prefab, WorldObject worldObject)
    {
        Quaternion rot = Quaternion.Euler(0f, 0, 0f);
        var obj = Instantiate(prefab, new Vector2(worldObject.Position.x, worldObject.Position.y), rot);
        obj._worldObject = worldObject;
        return obj;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ClientPlayerView playerView = other.GetComponent<ClientPlayerView>();

        if (playerView != null)
            playerView.PickupObject(_worldObject.Id);
    }

    // Start is called before the first frame update
    void Start()
    {
        // in case we want to throw one into a level conventionally
        if (_worldObject == null)
        {
            _worldObject = new WorldObject();
            _worldObject.SetPosition(new WorldVector(transform.position.x, transform.position.y));
        }

        _renderer = GetComponent<SpriteRenderer>();

        switch (_worldObject.Type)
        {
            case ObjectType.KeyRed:
                _renderer.color = new Color(0xFF, 0x00, 0x00);
                break;
            case ObjectType.KeyGreen:
                _renderer.color = new Color(0x00, 0xFF, 0x00);
                break;
            case ObjectType.KeyBlue:
                _renderer.color = new Color(0x00, 0x00, 0xFF);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void IObjectView.OnRelease(IPlayerView playerView)
    {

    }

    ObjectType IObjectView.GetObjectType()
    {
        return _worldObject.Type;
    }

    void IObjectView.OnActivate(IPlayerView playerView)
    {
        // should be called by client on confirmation of activation by server
    }

    void IObjectView.Destroy()
    {
        Destroy(gameObject);
    }

    void IObjectView.Update(WorldObject worldObject, ushort tick)
    {
        _worldObject = worldObject;
        gameObject.SetActive(_worldObject.IsActive);
        transform.position.Set(_worldObject.Position.x, _worldObject.Position.x, 0);
    }

    int IObjectView.GetId()
    {
        return _worldObject.Id;
    }

    void IObjectView.SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

}
