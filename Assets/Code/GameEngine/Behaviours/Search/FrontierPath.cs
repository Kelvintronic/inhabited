using System;

namespace GameEngine.Search
{
    public class FrontierPath : IComparable<FrontierPath>
    {
        private static uint _counter;

        private readonly Path _path;
        private readonly double _costEstimate;
        private readonly uint _sequence;

        /// <summary>
        /// Contructs a new <c>FrontierPath</c> object suitable for use in <c>PriorityQueue</c>.
        /// The <c>Path</c> will be cloned to a new instance
        /// </summary>
        /// <param name="path">the <c>Path</c> to be cloned</param>
        /// <param name="heuristic">the heuristic estimate from end of path to goal</param>
        public FrontierPath(Path path, double heuristic)
        {
            _path = path.Clone();
            _costEstimate = _path.Cost + heuristic;
            _sequence = _counter++;
        }

        public Path Path
        {
            get => _path;
        }

        public int CompareTo(FrontierPath other)
        {
            // return < 0 for this precedes other in sort order
            // return 0 for same position in sort order
            // return > 0 for this follows other in sort order

            int res = this._costEstimate.CompareTo(other._costEstimate);

            if (res == 0)
            {
                res = this._sequence.CompareTo(other._sequence);
            }

            return res;
        }
    }
}