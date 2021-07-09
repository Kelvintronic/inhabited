using GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{

    public class ClientBolt : MonoBehaviour
    {
        void Start()
        {
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Wall"))
            {
                Debug.Log(gameObject.name + " hit a wall");
                Destroy(gameObject);
            }
            else if (other.CompareTag("Player"))
            {
                var playerView = other.GetComponentInParent<IPlayerView>();
                Debug.Log(gameObject.name + " hit player id '" + playerView.GetId() + "'");
            }
            else
            {
                Debug.Log(gameObject.name + " hit something tagged '" + other.tag + "'");
                Destroy(gameObject);
            }
        }
    }
}