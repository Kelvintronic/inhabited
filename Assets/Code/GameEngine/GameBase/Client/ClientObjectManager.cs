using System.Collections.Generic;

namespace GameEngine
{
    public struct ObjectHandler
    {
        public readonly WorldObject WorldObject;
        public readonly IObjectView View;

        public ObjectHandler(WorldObject worldObject, IObjectView view)
        {
            WorldObject = worldObject;
            View = view;
        }
    }

    public class ClientObjectManager : WorldObjectManagerBase
    {
        private readonly Dictionary<int, ObjectHandler> _worldObjects;
        private ClientPlayer _clientPlayer;

        public ClientPlayer OurPlayer => _clientPlayer;
        public override int Count => _worldObjects.Count;

        public ClientObjectManager()
        {
            _worldObjects = new Dictionary<int, ObjectHandler>();
        }

        public override IEnumerator<WorldObject> GetEnumerator()
        {
            foreach (var ph in _worldObjects)
                yield return ph.Value.WorldObject;
        }

        public void ApplyServerChange(ref WorldObject serverObjectState)
        {
            if (!_worldObjects.TryGetValue(serverObjectState.Id, out var handler))
                return;
            
            // TODO: Apply server state
        }

        public IObjectView GetViewById(int id)
        {
            return _worldObjects.TryGetValue(id, out var oh) ? oh.View : null;
        }

        public WorldObject GetObjectById(int id)
        {
            return _worldObjects.TryGetValue(id, out var oh) ? oh.WorldObject : null;
        }

        public WorldObject RemoveObject(int id)
        {
            if (_worldObjects.TryGetValue(id, out var handler))
            {
                _worldObjects.Remove(id);
                handler.View.Destroy();
            }
        
            return handler.WorldObject;

        }

        public void RemoveAll()
        {
            foreach(var worldObject in _worldObjects)
            {
                worldObject.Value.View.Destroy();
            }
            _worldObjects.Clear();
        }

        public override void LogicUpdate()
        {
           /* foreach (var kv in _players)
            {
                kv.Value.Update(LogicTimer.FixedDelta);
            }*/
        }

        public void AddWorldObject(WorldObject worldObject, IObjectView view)
        {
            _worldObjects.Add(worldObject.Id, new ObjectHandler(worldObject, view));
        }
        
    }
}