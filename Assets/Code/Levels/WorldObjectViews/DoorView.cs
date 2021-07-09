using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class DoorView : MonoBehaviour, IObjectView
{
    private SpriteRenderer _renderer;
    private WorldObject _worldObject;

    public static DoorView Create(DoorView prefab, WorldObject worldObject)
    {
        Quaternion rot = Quaternion.Euler(0f, 0, 0f);
        var obj = Instantiate(prefab, new Vector2(worldObject.Position.x, worldObject.Position.y), rot);
        obj._worldObject = worldObject;
        return obj;
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

        if (_worldObject.Width > 1)
        {
            // set size and adjust position to allow for new width
            if (_worldObject.IsHorizontal)
            {
                _renderer.size = new Vector2(_worldObject.Width, 1);
                gameObject.transform.position = new Vector2(gameObject.transform.position.x + (0.5f * _worldObject.Width)-0.5f,
                                                            gameObject.transform.position.y);
            }
            else
            {
                _renderer.size = new Vector2(1, _worldObject.Width);
                gameObject.transform.position = new Vector2(gameObject.transform.position.x,
                                                            gameObject.transform.position.y + (0.5f * _worldObject.Width)-0.5f);
            }
            
        }

        switch (_worldObject.Type)
        {
            case ObjectType.DoorRed:
                _renderer.color = new Color(0xFF, 0x00, 0x00);
                break;
            case ObjectType.DoorGreen:
                _renderer.color = new Color(0x00, 0xFF, 0x00);
                break;
            case ObjectType.DoorBlue:
                _renderer.color = new Color(0x00, 0x00, 0xFF);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void IObjectView.Destroy()
    {
        Destroy(gameObject);
    }

    ObjectType IObjectView.GetObjectType()
    {
        return _worldObject.Type;
    }

    void IObjectView.OnActivate(IPlayerView playerView)
    {
        // TODO: Implement action
    }

    void IObjectView.OnRelease(IPlayerView playerView)
    {

    }

    void IObjectView.Update(WorldObject worldObject, ushort tick)
    {
        _worldObject = worldObject;

        // And do any updates required to view here
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
