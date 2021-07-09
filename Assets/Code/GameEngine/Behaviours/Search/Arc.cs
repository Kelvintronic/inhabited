using UnityEngine;

namespace GameEngine.Search
{
    public struct Arc
    {
        public readonly Vector2Int tail;
        public readonly Vector2Int head;
        public readonly string label;
        public readonly double cost;
        public readonly bool isTransientObstacle;

        public Arc(Vector2Int tail, Vector2Int head, string label, double cost, bool isTransientObstacle = false)
        {
            this.tail = tail;
            this.head = head;
            this.label = label;
            this.cost = cost;
            this.isTransientObstacle = isTransientObstacle;
        }
    }
}