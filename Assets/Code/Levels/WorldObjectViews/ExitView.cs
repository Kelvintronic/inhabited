using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class ExitView : MonoBehaviour, IObjectView
{

    private int _id;
    private int _viewIndex;

    [SerializeField] private GameObject[] _views;

    private WorldObject _worldObject;

    public static ExitView Create(ExitView prefab, WorldObject worldObject, bool isServer)
    {
        var view = Instantiate<ExitView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y), Quaternion.identity);
        view._worldObject = worldObject;
        return view;
    }

    // Start is called before the first frame update
    void Start()
    {
        _id = _worldObject.Id;
        _viewIndex = _worldObject.Flags;
        for (byte i = 0; i < _views.Length; i++)
            _views[i].SetActive(i == _viewIndex);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    ObjectType IObjectView.GetObjectType()
    {
        return ObjectType.ExitPoint;
    }

    void IObjectView.OnActivate(IPlayerView playerView)
    {
        // should be called by client on confirmation of activation by server
    }

    void IObjectView.OnRelease(IPlayerView playerView)
    {

    }

    void IObjectView.Destroy()
    {
        Destroy(gameObject);
    }

    void IObjectView.Update(WorldObject worldObject, ushort tick)
    {
        // Server has changed something so use data
        // to update view here.
    }

    int IObjectView.GetId()
    {
        return _id;
    }

    void IObjectView.SetActive(bool isActive)
    {
        gameObject.SetActive(false);
    }

    GameObject IObjectView.GetGameObject()
    {
        return gameObject;
    }
}
