using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class NPCSpider : NPC
    {

        // Search variables
        private bool _useSearch { get { return _aStarSearch != null; } } // if level does not contain AStar this will be false
        private ISearchJob _plannedPathJob;
        private int _pathStep;
        private bool _isNextMoveOnPath;
        private bool _pauseSearching = false;

        public NPCSpider(WorldVector position, INotificationManager manager) : base(position, manager)
        {
            _canHit = true;
            _type = ObjectType.NPCSpider;
            _speed = 2;
            health = 5;        
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
                        // populate _nextCell based on method
                        if (_useSearch && !_pauseSearching)
                            _hasIntent = GetNextPathMove();
                        else 
                            _hasIntent = GetNextMove(); // head straight toward player
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
                                ClearPathJob();
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
                health--;
                Debug.Log("Mercenary id '" + Id + "' lost health");
            }
            else
                return true;

            return false;
        }


        public override void Destroy()
        {
            base.Destroy();
        }

        private bool GetNextPathMove()
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
                    _isNextMoveOnPath = false;
                    return GetNextMove();       // no change in transients blocking path
                }

            }

            if (_plannedPathJob != null)
            {
                // there is a path job, maybe not completed
                if (_plannedPathJob.IsGoal(targetCell)) // is the goal of the path the same as before?
                {
                    // yes - the closest player is still at the position we're going for
                    if (_plannedPathJob.HasSolution && _plannedPathJob.IsFinished)
                    {

                        // we are already following the solved path, try to continue
                        return ContinueOnPath();

                    }
                    else
                    {
                        if (_plannedPathJob.IsFinished)
                        {
                            // job is finished without a solution

                            // seek a new path
                            ClearPathJob();

                            // move toward the player, in a good old fashioned straight line
                            _isNextMoveOnPath = false;
                            return GetNextMove();

                        }

                        // job is not finished so do nothing
                    }
                }
                else
                {
                    // path was bad and player has moved
                    ClearPathJob();
                    _isNextMoveOnPath = false;
                    return GetNextMove();
                }
            }
            else
            {
                Debug.Log("Adding new search job");
                // submit a new search job
                ClearPathJob();
                var newJob = new AStarSearchJob(_currentCell, targetCell);
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
        /// Provide the next cell if current cell is the expected cell on the path.
        /// </summary>
        /// <param name="currentCell">map cell to path from</param>
        private bool ContinueOnPath()
        {
            // Check if we've reached the end of the path
            // (end being the final tail, or second to last head)
            if (_currentCell.Equals(_plannedPathJob.Solution.End().head))
            {
                // it's the end of the path, clear the plan to trigger a new search
                ClearPathJob();
                return false;
            }
            else
            {
                // confirm last provided cell has actually been reached and record the step.
                var expectedCell = _plannedPathJob.Solution.ArcsList[_pathStep+1].head;
                if (_currentCell.Equals(expectedCell))
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
        }

        public override void DestroyNotification()
        {
            _pauseSearching = false;

            base.DestroyNotification();
        }

    }
}