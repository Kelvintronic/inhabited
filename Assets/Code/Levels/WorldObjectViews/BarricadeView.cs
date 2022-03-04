using UnityEngine;

namespace GameEngine
{
    public class BarricadeView : MonoBehaviour, IObjectView
    {
        private int _id;
        private WorldObject _worldObject;

        public int Id { get => _id; }

        public static BarricadeView Create(BarricadeView prefab, WorldObject worldObject)
        {
            var view = Instantiate<BarricadeView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y), Quaternion.identity);
            view._worldObject = worldObject;
            return view;
        }

        private void Awake()
        {
        }

        // Start is called before the first frame update
        private void Start()
        {
        }

        private void Update()
        {
        }

        ObjectType IObjectView.GetObjectType()
        {
            return _worldObject.Type;
        }

        int IObjectView.GetId()
        {
            return _id;
        }

        void IObjectView.OnActivate(IPlayerView playerView)
        {
            // should be called by client on confirmation of activation by server
        }

        void IObjectView.OnRelease(IPlayerView playerView)
        {
        }

        void IObjectView.Destroy()
        {
            Destroy(gameObject);
        }

        void IObjectView.Update(WorldObject worldObject, ushort tick)
        {
            // Server has changed something so use data
            // to update view here.
            _worldObject = worldObject;

            this.transform.rotation = Quaternion.Euler(0f, 0f, _worldObject.Rotation);
        }

        void IObjectView.SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        GameObject IObjectView.GetGameObject()
        {
            return gameObject;
        }

    }
}