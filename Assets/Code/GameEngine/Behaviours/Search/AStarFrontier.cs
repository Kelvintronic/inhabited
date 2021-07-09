using GameEngine.Search.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Search
{
    public class AStarFrontier
    {
        private readonly PriorityQueue<FrontierPath> _queue;
        private readonly HashSet<Vector2Int> _expanded;

        public AStarFrontier(Vector2Int startNode, double heuristic)
        {
            _queue = new PriorityQueue<FrontierPath>();
            _expanded = new HashSet<Vector2Int>();

            var firstPath = new Path();
            firstPath.Add(new Arc(new Vector2Int(int.MinValue, int.MinValue), startNode, "START", 0));
            Add(firstPath, heuristic);
        }

        public void Add(Path path, double heuristic)
        {
            _queue.Enqueue(new FrontierPath(path, heuristic));
        }

        public IEnumerable<Path> Paths()
        {
            while (_queue.Count() > 0)
            {
                var popped = _queue.Dequeue();

                if (_expanded.Contains(popped.Path.End().head))
                {
                    continue;
                }

                _expanded.Add(popped.Path.End().head);

                yield return popped.Path;
            }
        }
    }
}