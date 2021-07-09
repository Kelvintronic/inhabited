using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameEngine.Search
{
    /// <summary>
    /// Container for a specific A* search job
    /// </summary>
    public class AStarSearchJob : ISearchJob
    {
        private readonly Vector2Int _startNode;
        private readonly Vector2Int _goalNode;
        private bool _isCancelled;
        private Path _solution;

        public Vector2Int StartNode => _startNode;
        public Vector2Int GoalNode => _goalNode;
        public bool IsCancelled => _isCancelled;
        public bool IsFinished { get; set; }
        public bool HasSolution { get; set; }
        public bool HasPathBlockedByTransient { get => _transientList.Count > 0; }

        private List<Vector2Int> _transientList;
        public Path Solution
        {
            get => _solution;
            set => _solution = value;
        }

        public AStarSearchJob(Vector2Int start, Vector2Int goal)
        {
            _startNode = start;
            _goalNode = goal;
            _isCancelled = false;
            IsFinished = false;
            HasSolution = false;
            _transientList = new List<Vector2Int>();
            _solution = new Path();
        }

        public void AddTransient(Vector2Int transient)
        {
            if (!_transientList.Exists(t => t == transient))
            {
                _transientList.Add(transient);
                Debug.Log("Transient Added");
            }

        }

        /// <summary>
        /// Returns all arcs containing transient obstacles
        /// </summary>
        public IEnumerable<Vector2Int> Transients()
        {
            foreach (var transient in _transientList)
            {
                yield return transient;
            }
        }

        public void Cancel()
        {
            _isCancelled = true;
        }

        public bool IsGoal(Vector2Int node)
        {
            return node == _goalNode;
        }

        /// <summary>
        /// Heuristic function for A star search strategy. Uses Chebyshev distance
        /// </summary>
        /// <param name="node">starting point for cost estimate</param>
        public double EstimatedCostToGoal(Vector2Int node)
        {
            // Euclidean distance (slight overestimate due to diagonal moves only costing 1)
            // return Math.Sqrt(Math.Pow(GoalNode.x - node.x, 2) + Math.Pow(GoalNode.y - node.y, 2));

            // Chebyshev distance (underestimate to ensure A* admissibility)
            return Math.Max(Math.Abs(_goalNode.x - node.x), Math.Abs(_goalNode.y - node.y));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (!IsFinished)
            {
                sb.Append("Search job for ")
                    .Append(_startNode).Append(" -> ").Append(_goalNode)
                    .Append(" has not been processed yet");
            }
            else
            {
                if (!HasSolution)
                {
                    sb.Append("There is no solution for ")
                        .Append(_startNode).Append(" -> ").Append(_goalNode);
                }
                else
                {
                    sb.Append("Solution for ")
                        .Append(_startNode).Append(" -> ").Append(_goalNode)
                        .Append(" costs: ").Append(_solution.Cost)
                        .Append("\n");

                    var first = true;
                    foreach (var arc in _solution.Arcs())
                    {
                        if (first)
                        {
                            // skip first Arc since it's the free move to START NODE
                            first = false;
                            continue;
                        }

                        sb.Append(arc.label).Append(" ");
                    }
                }
            }

            return sb.ToString();
        }
    }
}