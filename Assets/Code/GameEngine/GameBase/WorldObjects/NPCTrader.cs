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
        }

        public override bool Update(float delta)
        {

            _blockedPathTimer.UpdateAsCooldown(delta);

            if (_updateTimer.IsTimeElapsed)
                if (_isWatching)
                {
                    _updateTimer.Reset();

                    // get current cell
                    _currentCell = _mapArray.GetCellVector(_position);

                    if (!_hasIntent)
                    {
                        _hasIntent = GetNextMove(); // see if there is a move to make
                        _isMoving = false;
                    }

                    if (_hasIntent && !_isMoving)
                    {
                        if (!TryToMove())
                        {
                            // path is blocked so wait
                            if (_blockedPathTimer.IsTimeElapsed)
                            {
                                // if we've been waiting too long give up trying to move
                                _blockedPathTimer.Reset();
                                _hasIntent = false;
                                _isWatching = false;
                            }
                        }
                    }

                    // if moving is in progress, continue
                    if (_isMoving)
                    {
                        _position += _moveDelta;
                        _rotation = Mathf.Atan2(_moveDelta.y, _moveDelta.x) - 90 * Mathf.Deg2Rad;
                        _moveCount--;
                        _update = true;

                        // if client animation is complete:
                        if (_moveCount == 0)
                        {
                            _position = _intentVector; // ensure we have hit the spot
                            _mapArray.SetCell(_fromCell, MapCell.Empty); // empty the old cell
                            _hasIntent = false;
                            _isWatching = false;
                        }
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
                Debug.Log("Trader id '" + Id + "' lost health");
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