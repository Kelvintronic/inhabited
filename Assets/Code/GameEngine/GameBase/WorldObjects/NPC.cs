using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPC : ServerWorldObject
    {
        public const int Levels = 3; // number of different levels the NPC has
        public const int MaxSpeed = 3;
        private int _moveCount;
        private WorldVector _moveDelta;
        public NPCStance stance;
        [Range(0, Levels - 1)]
        public byte level;
        
        // ai movement variables
        private WorldVector _watching;       // location of object of intent
        private bool _isWatching = false;
        private bool _hasIntent = false;     // true if intentVector is valid
        private WorldVector _intentVector;   // destination location
        private Vector2Int _fromCell;         // current array cell
        private Vector2Int _toCell;           // destination array cell

        // Search variables
        private bool _useSearch { get { return _aStarSearch != null; } } // if level does not contain AStar this will be false
        // private GameTimer _noPathIdleTimer = new GameTimer(1.0f);
        private GameTimer _blockedPathTimer = new GameTimer(1.0f);
        private ISearchJob _plannedPathJob;
        private Vector2Int _nextCell;
        private int _pathStep;
        private bool _isNextMoveOnPath;
        private bool _pauseSearching = false;

        public WorldVector Watching { get => _watching; }

        public NPC(WorldVector position, ObjectType type, INotificationManager manager) : base(position,0.2f, manager)
        {
            _canHit = true;
            _type = type;
            switch (type)
            {
                case ObjectType.NPC_level3:
                    level = 2;
                    break;
                case ObjectType.NPC_level2:
                    level = 1;
                    break;
                default:
                    level = 0;
                    _type = ObjectType.NPC_level1;
                    break;
            }
            _speed = level + 1;
        }

        public override bool Update(float delta)
        {

            base.Update(delta);

           // _noPathIdleTimer.UpdateAsCooldown(Time.deltaTime);
            _blockedPathTimer.UpdateAsCooldown(Time.deltaTime);

            if (_updateTimer.IsTimeElapsed)
            {
                _updateTimer.Reset();
                if (_hasIntent)
                {
                    _position += _moveDelta;
                    _moveCount--;
                    if (_moveCount == 0)
                    {
                        _position = _intentVector; // ensure we have hit the spot
                        _mapArray.SetCell(_fromCell, MapCell.Empty); // empty the old cell
                        _hasIntent = false;
                        UpdateMovement();
                    }

                    // Intercell animation is incomplete
                    return true;
                }
                else
                {
                    UpdateMovement();
                }

            }

            return false;
        }

        public void SetWatching(WorldVector watching)
        {
            if (!_hasIntent)
            {
                _watching = watching;
                _isWatching = true;
            }
        }

        public void UpdateMovement()
        {
            // if watching is invalid do nothing
            if (!_isWatching)
                return;

            if (!_hasIntent)
            {
                var currentCell = _mapArray.GetCellVector(_position);

                // populate _nextCell based on method
                if (_useSearch&&!_pauseSearching)
                {
                    if (!GetNextMove(new Vector2Int(currentCell.x, currentCell.y)))
                        return; // no move available so do nothing
                }
                else // head straight toward player
                {
                    if (!GetDefaultNextMove(new Vector2Int(currentCell.x, currentCell.y)))
                        return; //  no move available so do nothing
                }

                // check to see if there is something in the toCell location
                if (_mapArray.Array[_nextCell.x, _nextCell.y].type == ObjectType.None)
                {
                    _fromCell = currentCell;
                    _toCell = _nextCell;
                    _mapArray.Array[_nextCell.x, _nextCell.y] = new MapCell { type = ObjectType.NPC_Intent, id = Id };

                    _intentVector = _mapArray.GetWorldVector(_toCell.x, _toCell.y);

                    _moveCount = MaxSpeed - _speed + 1;
                    _moveDelta = (_intentVector - _position) / _moveCount;
                    _hasIntent = true;

                    _blockedPathTimer.Reset();
                }
                else
                {
                    // if path is blocked wait
                    if(_blockedPathTimer.IsTimeElapsed)
                    {
                        _blockedPathTimer.Reset();

                        // then only if move is from proposed path shall we cancel it
                        if(_isNextMoveOnPath)
                            ClearPathJob();
                    }
                }
            }
        }

        public override bool OnHit()
        {
            if (level > 0) 
            {
                level--;
                _type = ObjectType.NPC_level1 + level;
                Debug.Log("NPC id '" + Id + "' devolved to level " + level);
                // _speed = level + 1;
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

        private bool GetNextMove(Vector2Int currentCell)
        {
            _isNextMoveOnPath = true;

            var targetCell = _mapArray.GetCellVector(_watching);

            // Note: It seems that the end condition for the search is set to one cell back along the path from the end.
            //       This means that the NPC sometimes won't get close enough to hit the player but still stops. A new seach
            //       then begins that also ends one cell back from the end so the NPC simply doesn't move. To get over this 
            //       problem, when the end is detected in ContinueOnPath, GenNextDefaultMove is called to get the NPC to make
            //       the last step. This may push the player causing a new watching to be set and a new job to be submitted 
            //       that also cannot end unless we check the following condition here and call ClearPathJob.
            /*            if (targetCell == currentCell)
                        {
                            // we have come to the end of our path so do nothing until our eyes spy another player
                            ClearPathJob();
                            return false;
                        }*/
            if (_plannedPathJob != null)
            {
                if (_plannedPathJob.IsFinished && !_plannedPathJob.HasSolution && _plannedPathJob.HasPathBlockedByTransient)
                {
                    Debug.Log("Tramnsients present");
                    int count=0;
                    if(_notificationManager!=null)
                        foreach (var cell in _plannedPathJob.Transients())
                        {
                            count++;
                            _notificationManager.AddDestroyNotification(Id,_mapArray.Array[cell.x, cell.y].id);
                        }
                    Debug.Log($"Transient destroy notifications added: {count}");
                    _pauseSearching = true;
                    ClearPathJob();
                    return GetDefaultNextMove(currentCell);       // no change in transients blocking path
                }

            }

            if (_plannedPathJob != null)
            {
                // there is a path job, maybe not completed
                if (_plannedPathJob.IsGoal(targetCell))
                {
                    // the closest player is still at the position we're going for
                    if (_plannedPathJob.HasSolution && _plannedPathJob.IsFinished)
                    {

                        // we are already following the solved path, try to continue
                        return ContinueOnPath(currentCell);

                    }
                    else
                    {
                        if (_plannedPathJob.IsFinished)
                        {
                            // job is finished without a solution

                            // seek a new path
                            ClearPathJob();

                            // move toward the player, in a good old fashioned straight line
                            return GetDefaultNextMove(currentCell);

                        }

                        // job is not finished so do nothing
                    }
                }
                else
                {
                    // closest target is not at planned path goal

/*                    if (_plannedPathJob.IsFinished&&_plannedPathJob.HasSolution)
                    {
                        // path was good, but player has moved
                        // make the next move but then start a new search
                        var result = ContinueOnPath(currentCell);
                        ClearPathJob();
                        return result;
                    }*/

                    // path was bad and player has moved
                    ClearPathJob();
                    return GetDefaultNextMove(currentCell);
                }
            }
            else
            {
                Debug.Log("Adding new search job");
                // submit a new search job
                ClearPathJob();
                var newJob = new AStarSearchJob(currentCell, targetCell);
                var pendingJobs = _aStarSearch.AddJob(newJob);
                if (pendingJobs == -1)
                {
                    Debug.Log($"Monster [id:{Id}] search job rejected");
                }
                else
                {
                    _plannedPathJob = newJob;
                }
            }

            // no move available (yet)
            return false;
        }

        /// <summary>
        /// Attempts to populate private variable _nextCell with a different cell from the current cell
        /// Returns true on success
        /// </summary>
        /// <param name="currentCell"></param>
        /// <returns></returns>

        private bool GetDefaultNextMove(Vector2Int currentCell)
        {
            _isNextMoveOnPath = false;

            // snap watching to grid
            var targetCell = _mapArray.GetCellVector(_watching);
            var targetVector = _mapArray.GetWorldVector(targetCell.x, targetCell.y);

            // Start from current cell
            Vector2Int toCell = currentCell;

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

            if(toCell!=currentCell)
            {
                _nextCell = toCell;
                return true;
            }

            // the watching cell is the same as our cell so invalidate watching
            _isWatching = false;

            return false;
        }

        /// <summary>
        /// Provide the next cell if current cell is the expected cell on the path.
        /// </summary>
        /// <param name="currentCell">map cell to path from</param>
        private bool ContinueOnPath(Vector2Int currentCell)
        {
            // Check if we've reached the end of the path
            // (end being the final tail, or second to last head)
            if (currentCell.Equals(_plannedPathJob.Solution.End().head))
            {
                // it's the end of the path, clear the plan to trigger a new search
                ClearPathJob();
                return false;
            }
            else
            {
                // confirm last provided cell has actually been reached and record the step.
                var expectedCell = _plannedPathJob.Solution.ArcsList[_pathStep+1].head;
                if (currentCell.Equals(expectedCell))
                    _pathStep++;

                // provide intended next cell
                _nextCell=_plannedPathJob.Solution.ArcsList[_pathStep+1].head;
                return true;
            }
        }

        private void ClearPathJob()
        {
            if(_plannedPathJob!=null)
                _plannedPathJob.Cancel();
            _plannedPathJob = null;
            _pathStep = 0;
            _isWatching = false;
        }

        public override void DestroyNotification()
        {
            _pauseSearching = false;
        }

    }
}