using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    public abstract class WorldObjectManagerBase : IEnumerable<WorldObject>
    {
        public abstract IEnumerator<WorldObject> GetEnumerator();
        public abstract int Count { get; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void LogicUpdate();
    }
}