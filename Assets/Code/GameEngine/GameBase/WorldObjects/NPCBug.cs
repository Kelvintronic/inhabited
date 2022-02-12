﻿using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPCBug : NPC
    {
        public NPCBug(WorldVector position, INotificationManager manager) : base(position, manager)
        {
            _canHit = true;
            _type = ObjectType.NPCBug;
            _speed = 3;
        }

        public override bool Update(float delta)
        {

            // _noPathIdleTimer.UpdateAsCooldown(Time.deltaTime);
            _blockedPathTimer.UpdateAsCooldown(Time.deltaTime);

            if (_updateTimer.IsTimeElapsed)
            {
                _updateTimer.Reset();
                if (_hasIntent)
                {
                    _position += _moveDelta;
                    _moveCount--;
                    _update = true;


                    // if client animation is complete:
                    if (_moveCount == 0)
                    {
                        _position = _intentVector; // ensure we have hit the spot
                        _mapArray.SetCell(_fromCell, MapCell.Empty); // empty the old cell
                        _hasIntent = false;
                        UpdateMovement();
                    }
                }
                else
                {
                    UpdateMovement();
                }

            }

            // Don't forget to set the boolean: _update=true if you need the client to be updated
            // Note: The client only gets the WorldObject base data
            return base.Update(delta);
        }

        public override bool OnHit()
        {
            if (health > 0) 
            {
                health --;
                Debug.Log("Bug id '" + Id + "' lost health");
            }
            else
                return true;

            return false;
        }

        public override void DestroyNotification()
        {
            base.DestroyNotification();
        }

    }
}