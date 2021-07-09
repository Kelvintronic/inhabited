using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class SpawnPoint : MonoBehaviour, IObjectView
{
    private int _id;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    ObjectType IObjectView.GetObjectType()
    {
        return ObjectType.None;
    }

/*    void IObjectView.OnHit(byte playerId)
    {
        // this object does not react to hits
    }*/

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

}
