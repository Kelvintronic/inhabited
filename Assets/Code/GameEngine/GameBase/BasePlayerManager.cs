using System.Collections;
using System.Collections.Generic;

namespace GameEngine
{
    public abstract class BasePlayerManager : IEnumerable<BasePlayer>
    {
        public abstract IEnumerator<BasePlayer> GetEnumerator();
        public abstract int Count { get; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void LogicUpdate();
    }
}