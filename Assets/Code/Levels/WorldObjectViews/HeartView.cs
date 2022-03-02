using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class HeartView : MonoBehaviour, IObjectView
{
    private SpriteRenderer _renderer;
    private WorldObject _worldObject;

    public static HeartView Create(HeartView prefab, WorldObject worldObject)
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

    GameObject IObjectView.GetGameObject()
    {
        return gameObject;
    }

}
