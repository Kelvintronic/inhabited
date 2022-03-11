using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPCMantis : NPC
    {
        public NPCMantis(WorldVector position, INotificationManager manager) : base(position, manager)
        {
            _canHit = true;
            _type = ObjectType.NPCMantis;
            _speed = 3;
            _health = 3;
        }

        public override bool Update(float delta)
        {

            _blockedPathTimer.UpdateAsCooldown(delta);

            if(_updateTimer.IsTimeElapsed)
            {
                _updateTimer.Reset();

                if (_isWatching)
                {
                    // get current cell
                    _currentCell = _mapArray.GetCellVector(_position);

                    if (!_hasIntent)
                    {
                        _hasIntent = GetNextMove(); // see if there is a move to make
                        _isMoving = false;
                        _flags = 0; // isMoving flag for client
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