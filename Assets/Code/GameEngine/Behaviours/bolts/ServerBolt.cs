using GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{

    public class ServerBoltArg : EventArgs
    {
        // if player Id == -1 then targetId is not a player
        public int playerId;
        public int targetId;
        public bool isTargetPlayer;
        public int damageFactor;
    }
    public class ServerBolt : MonoBehaviour
    {
        public int playerId; // set to -1 for non-player bolt
        public int damageFactor = 1;
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
                if (playerView != null)
                {
                    var e = new ServerBoltArg { playerId = this.playerId, targetId = playerView.GetId(),
                                                isTargetPlayer = true, damageFactor = this.damageFactor };
                    hitHandler.Invoke(this, e);
                }
                Debug.Log(OwnerInfo() + gameObject.name + " hit player id '" + playerView.GetId() + "'");
                Destroy(gameObject);
            }
            else
            {
                var objectView = other.GetComponentInParent<IObjectView>();

                if(objectView!=null)
                {
                    var e = new ServerBoltArg { playerId = this.playerId,targetId = objectView.GetId(), 
                                                isTargetPlayer = false, damageFactor = this.damageFactor };
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