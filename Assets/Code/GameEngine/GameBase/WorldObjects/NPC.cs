using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public abstract class NPC : ServerWorldObject
    {
        public const int MaxHealth = 10; // number of different levels the NPC has
        public const int MaxSpeed = 3;

        public NPCStance stance;

        protected int _health;

        protected int _moveCount;
        protected WorldVector _moveDelta;
        
        // ai movement variables
        protected WorldVector _watching;       // location of object of intent
        protected bool _isWatching = false;
        protected bool _hasIntent = false;     // true if intentVector is valid
        protected bool _isMoving = false;       // true if a movement transition is occurring
        protected WorldVector _intentVector;   // destination location
        protected Vector2Int _currentCell;      // where we are in the map array
        protected Vector2Int _fromCell;         // current array cell
        protected Vector2Int _toCell;           // destination array cell
        protected Vector2Int _nextCell;
        protected GameTimer _blockedPathTimer = new GameTimer(1.0f);

        public WorldVector Watching { get => _watching; }

        public NPC(WorldVector position, INotificationManager manager) : base(position,0.2f, manager)
        {
        }

        public void SetWatching(WorldVector watching)
        {
            _watching = watching;
            _isWatching = true;
        }

        public override bool Update(float delta)
        {
            return base.Update(delta);
        }

        public override bool OnHit(int playerId = -1)
        {
            if (_health > 0) 
            {
                _health --;
                Debug.Log("NPC id '" + Id + "' lost health");
            }
            else
                return true;

            return false;
        }

        public override void Destroy()
        {
            if (_hasIntent)
            {
                _mapArray.SetCell(_fromCell, MapCell.Empty);
                _mapArray.SetCell(_toCell, MapCell.Empty);
            }
            
            base.Destroy();
        }

        /// <summary>
        /// Attempts to populate private variable _nextCell with a different cell from the current cell
        /// Returns true on success
        /// </summary>
        /// <param name="currentCell"></param>
        /// <returns></returns>

        protected virtual bool GetNextMove()
        {
            _blockedPathTimer.Reset();

            // snap watching to grid
            var targetCell = _mapArray.GetCellVector(_watching);
            var targetVector = _mapArray.GetWorldVector(targetCell.x, targetCell.y);

            // Start from current cell
            Vector2Int toCell = _currentCell;

            // convert absolute watching to relative
            var velocity = (targetVector - _position).Normalize(); ;

            // modify toCell based on velocity
            if (velocity.x < -0.5f)
                toCell.x--;
            if (velocity.x > 0.5f)
                toCell.x++;
            if (velocity.y < -0.5f)
                toCell.y--;
            if (velocity.y > 0.5f)
                toCell.y++;

            if (toCell != _currentCell)
            {
                _nextCell = toCell;
                return true;
            }

            return false;
        }

        protected virtual bool TryToMove()
        {

            // check to see if there is something in the toCell location
            if (_mapArray.Array[_nextCell.x, _nextCell.y].type == ObjectType.None)
            {
                _fromCell = _currentCell;
                _toCell = _nextCell;

                // claim the destination cell for ourselves
                _mapArray.Array[_nextCell.x, _nextCell.y] = new MapCell { type = ObjectType.NPC_Intent, id = Id };

                _intentVector = _mapArray.GetWorldVector(_toCell.x, _toCell.y);

                _moveCount = MaxSpeed - _speed + 1;
                _moveDelta = (_intentVector - _position) / _moveCount;
                _isMoving = true;

                return true;
            }

            return false;
        }

        public override void DestroyNotification()
        {
        }

    }
}