using GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{

    public class ServerBoltArg : EventArgs
    {
        public byte playerId;
        public int objectId;
    }
    public class ServerBolt : MonoBehaviour
    {
        public byte playerId;

        public event EventHandler<ServerBoltArg> hitHandler;

        void Start()
        {
           // _owner = GetComponent<PlayerOwned>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Wall"))
            {
                Debug.Log(OwnerInfo() + gameObject.name + " hit a wall");
                Destroy(gameObject);
            }
            else if (other.CompareTag("Player"))
            {
                var playerView = other.GetComponentInParent<IPlayerView>();

                Debug.Log(OwnerInfo() + gameObject.name + " hit player id '" + playerView.GetId() + "'");
            }
            else
            {
                var objectView = other.GetComponentInParent<IObjectView>();

                if(objectView!=null)
                {
                    var e = new ServerBoltArg { playerId = this.playerId,objectId = objectView.GetId() };
                    hitHandler.Invoke(this, e);
                }
                Debug.Log(OwnerInfo() + gameObject.name + " hit something tagged '" + other.tag + "'");
                Destroy(gameObject);
            }
        }

        private string OwnerInfo()
        {
            return ("player id " + playerId);
        }
    }
}