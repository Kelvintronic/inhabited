using GameEngine;
using GameEngine.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
     public class Gun : MonoBehaviour
    {
        [SerializeField] private GameObject _serverProjectilePrefab;
        [SerializeField] private GameObject _clientProjectilePrefab;


        // Start is called before the first frame update
        private void Start()
        {

        }

        private void Update()
        {

        }

        public void Shoot(bool isServer, ShootPacket shotData, EventHandler<ServerBoltArg> hitHandler)
        {
            var shotSpawnRot = Quaternion.Euler(0f, 0f, shotData.Direction * Mathf.Rad2Deg);
            var shotSpawnPos = transform.position + (shotSpawnRot * Vector3.up);
            GetComponent<AudioSource>().Play();

            if (isServer)
            {
                var bolt = Instantiate(_serverProjectilePrefab, shotSpawnPos, shotSpawnRot);
                var boltScript = bolt.GetComponent<ServerBolt>();
                if (shotData.IsNPCShooter)
                    boltScript.playerId = -1;
                else
                    boltScript.playerId = shotData.ShooterId;
                boltScript.hitHandler += hitHandler;
                boltScript.damageFactor = shotData.DamageFactor;
            }
            else
                Instantiate(_clientProjectilePrefab, shotSpawnPos, shotSpawnRot);
        }
    }
}
