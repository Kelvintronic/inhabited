using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPCMercenary : NPC
    {
        private bool _isHostile = false;
        public NPCMercenary(WorldVector position, INotificationManager manager) : base(position, manager)
        {
            _canHit = true;
            _type = ObjectType.NPCMercenary;
            _speed = 2;
            _health = 5;        
        }

        public override bool Update(float delta)
        {
            _blockedPathTimer.UpdateAsCooldown(delta);

            if (_updateTimer.IsTimeElapsed)
            {
                _updateTimer.Reset();

                if (_isWatching)
                {
                    var watchingDelta = _watching - _position;
                    _rotation = Mathf.Atan2(watchingDelta.y, watchingDelta.x) - 90 * Mathf.Deg2Rad;
                    _update = true;
                }
            }

            // Don't forget to set the boolean: _update=true if you need the client to be updated
            // Note: The client only gets the WorldObject base data
            return base.Update(delta);
        }

        public override bool OnHit(int playerId = -1)
        {
            if (playerId >= 0)
                SetFlag(Flag.IsHostile, true);

            return base.OnHit();
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void DestroyNotification()
        {
            base.DestroyNotification();
        }

    }
}