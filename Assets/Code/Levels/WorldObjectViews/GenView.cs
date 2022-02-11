using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class GenView : MonoBehaviour, IObjectView
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

        void Awake()
        {
            _clientLogic = GameObject.FindObjectOfType<ClientLogic>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _id = _worldObject.Id;
            SetLevel(_worldObject.Flags);
        }

        public void SetLevel(byte newSize)
        {
            if (newSize < MaxSize)
            {
                _size = newSize;
                for (byte i=0; i<MaxSize; i++)
                {
                    _levels[i].SetActive(i == _size);
                }
                _rigidbody2d = _levels[_size].GetComponent<Rigidbody2D>();
            }
        }


        public static GenView Create(GenView prefab, WorldObject worldObject, bool isServer)
        {
            var gen = Instantiate<GenView>(prefab, new Vector3(worldObject.Position.x, worldObject.Position.y),Quaternion.identity); 
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

            SetLevel(_worldObject.Flags);
        }

        void IObjectView.SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

    }
}
