using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;
using System;

public class AttackEventArgs : EventArgs
{
    public byte playerId;
    public int attackerId;
}

public class TelePort : MonoBehaviour
{
    public GameObject Destination;

    private ServerLogic _serverLogic;
    private WorldVector _destination;
    private GameTimer _teleportdelay = new GameTimer(1.0f);

    void Start()
    {
        _serverLogic = FindObjectOfType<ServerLogic>();
        _destination = new WorldVector(Destination.transform.position.x, Destination.transform.position.y);

        // hide layout sprites
        var fromSprite = GetComponent<SpriteRenderer>();
        fromSprite.enabled = false;
        var toSprite = Destination.GetComponent<SpriteRenderer>();
        toSprite.enabled = false;
    }


    private void Update()
    {
        _teleportdelay.UpdateAsCooldown(Time.deltaTime);
    }


    void OnTriggerStay2D(Collider2D other)
    {
        // Sensors only detect objects on the NPC or Player layers
        // this is set in Unity project settings for the physics engine

        if(_serverLogic.IsStarted&&_teleportdelay.IsTimeElapsed)
        {
            _teleportdelay.Reset();

            var clientPlayer = other.GetComponentInParent<ClientPlayerView>();
            if (clientPlayer != null)
            {
                Debug.Log("Client player triggered teleport");
                _serverLogic.OnTriggerTeleport(clientPlayer.GetId(), _destination);
                return;
            }

            var remotePlayer = other.GetComponentInParent<RemotePlayerView>();
            if (remotePlayer != null)
            {
                Debug.Log("Remote player triggered teleport");
                _serverLogic.OnTriggerTeleport(remotePlayer.GetId(), _destination);
            }

        }
    }
}
