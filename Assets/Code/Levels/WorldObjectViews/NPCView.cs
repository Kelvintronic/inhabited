using UnityEngine;

namespace GameEngine
{
    public class NPCView : MonoBehaviour, IObjectView
    {
        private int _id;
        [HideInInspector] public Rigidbody2D _rigidbody2d;
        [SerializeField] private GameObject[] _levels;

        private WorldObject _worldObject;
        private bool _isServer;
        private Monster _monster;

        public const int Levels = 3; // number of different levels the NPC has
        public NPCStance stance;

        [Range(0, Levels - 1)]
        public byte level;

        public int Id { get => _id; }
        public bool IsServer { get => _isServer; }
        public Monster Monster { get => _monster; }

        private void Awake()
        {
            _monster = GetComponent<Monster>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            _id = _worldObject.Id;

            level = (byte)(_worldObject.Type - ObjectType.NPC_level1);
            SetLevel(level);
        }

        public void SetLevel(byte newLevel)
        {
            if (newLevel < Levels)
            {
                level = newLevel;
                for (byte i = 0; i < Levels; i++)
                {
                    _levels[i].SetActive(i == level);
                }
                _rigidbody2d = _levels[level].GetComponent<Rigidbody2D>();
            }
        }

        public static NPCView Create(NPCView prefab, WorldObject worldObject, bool isServer)
        {
            var npc = Instantiate<NPCView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y), Quaternion.identity);
            //, Object.FindObjectOfType<ClientLogic>().levelSet.GetCurrentLevel().transform);
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

            Start();

            if (_monster != null)
                _monster.UpdatePosition(worldObject.Position);
        }

        void IObjectView.SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}