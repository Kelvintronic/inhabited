using System.Collections.Generic;

namespace GameEngine.Search
{
    public class Path
    {
        private readonly List<Arc> _arcs;
        private double _cost;

        public Path()
        {
            _arcs = new List<Arc>();
        }

        public void Add(Arc arc)
        {
            _arcs.Add(arc);
            _cost += arc.cost;
        }

        /// <summary>
        /// Returns sum of the costs of the <c>Arc</c>s currently in <c>Path</c>
        /// </summary>
        public double Cost
        {
            get => _cost;
        }

        public int Count
        {
            get => _arcs.Count;
        }

        public IReadOnlyList<Arc> ArcsList
        {
            get => _arcs.AsReadOnly();
        }

        /// <summary>
        /// Make an exact deep copy of <c>Path</c>
        /// </summary>
        /// <returns>reference to a new <c>Path</c> object</returns>
        public Path Clone()
        {
            var newPath = new Path();
            foreach (var arc in _arcs)
            {
                newPath.Add(arc);
            }

            return newPath;
        }

        /// <summary>
        /// Returns (via a generator) the sequence of <c>Arc</c> objects making up <c>Path</c>
        /// </summary>
        public IEnumerable<Arc> Arcs()
        {
            foreach (var arc in _arcs)
            {
                yield return arc;
            }
        }

        /// <summary>
        /// Throws exception if <c>Path</c> is empty
        /// </summary>
        /// <returns>the last <c>Arc</c> in <c>Path</c></returns>
        public Arc End()
        {
            return _arcs[_arcs.Count - 1];
        }
    }
}