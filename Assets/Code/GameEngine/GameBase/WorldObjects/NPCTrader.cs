using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPCTrader : NPC
    {
        public NPCTrader(WorldVector position, INotificationManager manager) : base(position, manager)
        {
            _canHit = true;
            _type = ObjectType.NPCTrader;
            _speed = 1;
            _health = 1;
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

        public override void DestroyNotification()
        {
            base.DestroyNotification();
        }

    }
}