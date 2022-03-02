using UnityEngine;

namespace GameEngine
{
    public class ConveyorView : MonoBehaviour, IObjectView
    {
        private int _id;
        private WorldObject _worldObject;
        private bool _isServer;
        private GameTimer _pushCooldownTimer;
        private float _speed;

        public int Id { get => _id; }
        public bool IsServer { get => _isServer; }

        public static ConveyorView Create(ConveyorView prefab, WorldObject worldObject, bool isServer)
        {
            var view = Instantiate<ConveyorView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y), Quaternion.identity);
            view._worldObject = worldObject;
            view._isServer = isServer;
            return view;
        }

        private void Awake()
        {
            _pushCooldownTimer = new GameTimer(0.1f);
            _speed = 0.2f;
            
        }

        // Start is called before the first frame update
        private void Start()
        {
            if(_worldObject!=null) // for manual placement of prefab
            {
                _id = _worldObject.Id;
                this.transform.rotation = Quaternion.Euler(0f, 0f, _worldObject.Rotation);
            }

        }

        private void Update()
        {
            _pushCooldownTimer.UpdateAsCooldown(Time.deltaTime);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            Debug.Log("Conveyor triggered: " + other);

            var player = other.GetComponentInParent<ClientPlayerView>();
            if (player != null)
            {
                if(_pushCooldownTimer.IsTimeElapsed)
                {
                    _pushCooldownTimer.Reset();
                    Vector2 velocity = this.transform.rotation * Vector2.left * _speed;
                    player.SetPositionCorrection(velocity);
                }
            }
            else
            {
            }
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