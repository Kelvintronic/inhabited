using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Search
{
    /// <summary>
    /// Interface for a generic search job
    /// </summary>
    public interface ISearchJob
    {
        Vector2Int StartNode { get; }
        Vector2Int GoalNode { get; }
        bool IsCancelled { get; }
        bool IsFinished { get; set; }
        bool HasSolution { get; set; }
        bool HasPathBlockedByTransient { get; }
        Path Solution { get; set; }

        void AddTransient(Vector2Int transient);
        IEnumerable<Vector2Int> Transients();
        void Cancel();
        bool IsGoal(Vector2Int node);

        double EstimatedCostToGoal(Vector2Int node);
    }
}