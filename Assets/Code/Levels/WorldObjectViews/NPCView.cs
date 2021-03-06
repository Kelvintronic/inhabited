using UnityEngine;

namespace GameEngine
{
    public class NPCView : MonoBehaviour, IObjectView
    {
        private int _id;

        [SerializeField] private GameObject _view;

        private WorldObject _worldObject;
        private bool _isServer;
        private Monster _monster;
        private Animator _animator;
        private GameTimer _animationRefreshTimer;

        private byte health;

        public int Id { get => _id; }
        public bool IsServer { get => _isServer; }
        public Monster Monster { get => _monster; }

        public bool IsHostile => _worldObject.GetFlag(Flag.IsHostile);

        private void Awake()
        {
            _monster = GetComponent<Monster>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            _id = _worldObject.Id;
            _animator = _view.transform.GetComponent<Animator>();
            _animationRefreshTimer = new GameTimer(1.0f);
        }

        private void Update()
        {
            _animationRefreshTimer.UpdateAsCooldown(Time.deltaTime);
            if (_animator != null)
            {
                if(_monster!=null)
                    if (_monster.IsMoving)
                    {
                        _animator.SetFloat("Speed", 1);
                        _animationRefreshTimer.Reset();
                    }
                    else
                    {
                        // delay stopping animation
                        if(_animationRefreshTimer.IsTimeElapsed)
                            _animator.SetFloat("Speed", 0);
                    }
            }
        }

        public static NPCView Create(NPCView prefab, WorldObject worldObject, bool isServer)
        {
            var npc = Instantiate<NPCView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y), Quaternion.identity);
            npc._worldObject = worldObject;
            npc._isServer = isServer;
            return npc;
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

            this.transform.rotation = Quaternion.Euler(0f, 0f, _worldObject.Rotation * Mathf.Rad2Deg);

            if (_monster != null)
            {
                _monster.UpdatePosition(_worldObject.Position);
            }
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