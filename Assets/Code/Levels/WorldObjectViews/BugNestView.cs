using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class BugNestView : MonoBehaviour, IObjectView
    {
        [HideInInspector] public ClientLogic _clientLogic;
        private int _id;
        [HideInInspector] public Rigidbody2D _rigidbody2d;
        [SerializeField] private GameObject[] _levels;

        private WorldObject _worldObject;
        private bool _isServer;

        public const int MaxSize = 3; // number of different sizes the Generator has
        private byte _size;

        public int Id { get => _id; }
        public bool IsServer { get => _isServer; }
        public bool IsOpen { get => _worldObject.Flags>0; }

        void Awake()
        {
            _clientLogic = GameObject.FindObjectOfType<ClientLogic>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _id = _worldObject.Id;
        }

        public static BugNestView Create(BugNestView prefab, WorldObject worldObject, bool isServer)
        {
            var gen = Instantiate<BugNestView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y),Quaternion.identity); 
            gen._worldObject = worldObject;
            gen._isServer = isServer;
            return gen;
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

            if(_worldObject.Flags==0)
            {
                _levels[0].SetActive(false);
                _levels[1].SetActive(true);
            }


        }

        void IObjectView.SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

    }
}
