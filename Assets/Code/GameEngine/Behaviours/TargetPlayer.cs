using GameEngine;
using GameEngine.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{  
    public class ShootEventArgs : EventArgs
    {
        public int shooterId;
    }
    public class TargetPlayer : MonoBehaviour
    {
        // cooldown timers
        private GameTimer _shootTimer = new GameTimer(1.0f);

        private Monster _monster;
        private NPCView _NPC;

        public event EventHandler<ShootEventArgs> Shoot;

        // Start is called before the first frame update
        private void Start()
        {
            _monster = GetComponentInParent<Monster>();
            _NPC = GetComponentInParent<NPCView>();
        }

        private void Update()
        {
            if (_NPC.IsServer)
            {
                _shootTimer.UpdateAsCooldown(Time.deltaTime);
                if(_shootTimer.IsTimeElapsed)
                {
                    _shootTimer.Reset();
                    if(CheckLineOfSite())
                    {
                        _monster.Shoot();
                    }
                }
            }
        }

        private bool CheckLineOfSite()
        {
            if(_monster!=null)
            {
                if(_monster.HasTarget)
                {
                    Vector2 lookDirection = this.transform.rotation * Vector2.up;
                    RaycastHit2D hit = Physics2D.Raycast(_monster.transform.position, lookDirection, _monster.Range, LayerMask.GetMask("Player"));
                    if (hit.collider != null)
                    {
                        Debug.Log("Raycast has hit the object " + hit.collider.gameObject);

                        IPlayerView thing = hit.collider.GetComponent<IPlayerView>();
                        if (thing != null)
                            return true;         
                    }
                }
            }
            return false;
        }
    }
}
