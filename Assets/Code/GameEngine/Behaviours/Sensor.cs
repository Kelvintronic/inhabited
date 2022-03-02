using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public struct SenseObject
{
    public bool isHumanPlayer;
    public int id;
    public Vector2 heading;        // where the item is in relation to us
    public Vector2 location;       // location of item

    public static SenseObject Empty
    {
        get
        {
            return new SenseObject
            {
                isHumanPlayer = false,
                id = -1,
                heading = Vector2.zero,
                location = Vector2.zero
            };
        }
    }
}

public class Sensor : MonoBehaviour
{
    
    public Queue<SenseObject> sensedObjects;
    private CircleCollider2D _collider;

    void Start()
    {
        sensedObjects = new Queue<SenseObject>();
        _collider = GetComponent<CircleCollider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Sensors only detect objects on the NPC or Player layers
        // this is set in Unity project settings for the physics engine

        //  Debug.Log("Object sensed: " + other);

        var component=other.GetComponentInParent<NPCView>();
        if (component != null)
        {
            AddSenseObject(other, false);
        }
        else
        {
            AddSenseObject(other, true);
        }
    }

    private void AddSenseObject(Collider2D other, bool isHumanPlayer)
    {
        SenseObject obj = new SenseObject
        {
            isHumanPlayer = isHumanPlayer
        };

        if (isHumanPlayer)
        {
            var player = other.GetComponentInParent<IPlayerView>();
            if (player != null)
            {
                obj.id = player.GetId();
            }
        }
        else
        {
            var nonHuman = other.GetComponentInParent<NPCView>();
            if (nonHuman != null)
            {
                obj.id = nonHuman.Id;
            }
        }

        obj.location = other.transform.position;
        obj.heading = other.transform.position - transform.position;
        sensedObjects.Enqueue(obj);
    }

    public void SetRadius(float radius)
    {
        if (radius > 20 || radius < 0)
            return;

        _collider.radius = radius;
    }

    public float GetRange()
    {
        if (_collider != null)
            return _collider.radius;
        return 0.0f;
    }

}
