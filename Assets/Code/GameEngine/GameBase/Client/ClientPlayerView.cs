using UnityEngine;
using UnityEngine.InputSystem;


namespace GameEngine
{
    public class ClientPlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private GameObject _sprite;
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _serverProjectilePrefab;
        [SerializeField] private GameObject _clientProjectilePrefab;
        [SerializeField] private GameObject _fogOfWarCircle;
        [SerializeField] private GameObject _childCollection;

        private Animator _animator;
        private const int _distanceOfSight = 6;
        private const int _targetSpeed = 2;

        private Vector2 _targetPosition;

        private ClientLogic _clientLogic;

        private Vector2 _velocity;
        private Vector2 _serverVelocity;
        private float _rotation;
        private Vector2 _lookDirection;
        private bool _fire;

        private GameTimer _enableInputTimer = new GameTimer(0.2f);
        private GameTimer _ignoreCollisionTimer = new GameTimer(0.1f);

        private ClientPlayer _player;

        // component references
        private Rigidbody2D _rigidbody2d;
        private Collider2D _collider2d;
        private SpriteRenderer _spriteRenderer;
        private HealthBar _healthBar;
        private SpriteMask _enemySpriteMask;

        private PlayerInputPacket _command;

        private bool _isInputDisabled = false;

        private bool _isFogOfWar = true;

        public bool IsFogOfWar => _isFogOfWar;

        public static ClientPlayerView Create(ClientLogic clientLogic, ClientPlayerView prefab, ClientPlayer player, Sprite sprite = null)
        {
            Quaternion rot = Quaternion.Euler(0f, 0, 0f);
            var obj = Instantiate(prefab, new Vector2(player.Position.x, player.Position.y), rot);
            obj._player = player;
            obj._clientLogic = clientLogic;

            if (sprite != null)
                obj.SetSprite(sprite);

            return obj;
        }

        private void Awake()
        {
            _rigidbody2d = GetComponent<Rigidbody2D>();
            _animator = _sprite.GetComponent<Animator>();
            //  _collider2d = gameObject.GetComponent<Collider2D>();
            //  _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            _enemySpriteMask = transform.Find("EnemySpriteMask").GetComponent<SpriteMask>();
        }

        public void SetSprite(Sprite sprite)
        {
           // _spriteRenderer.sprite = sprite;
        }

        public string GetStatusText()
        {
            return string.Format(
                        $"Speed: {_player.Speed}\n" +
                        $"Velocity x : {_velocity.x}\n" +
                        $"Velocity y : {_velocity.y}\n" +
                        $"Bag Items: {_player.GetBagContentString()}\n");
        }

        // Used by the player manager update to set the player command position
        // so the Unity clientview object determines the player position at the server
        public WorldVector GetActualPosition()
        {
            return new WorldVector(transform.position.x,transform.position.y);
        }

        private void Start()
        {
            _healthBar = GetComponentInChildren<HealthBar>();
            _enableInputTimer.Reset();
            Cursor.visible = false;
        }

        public void Move(InputAction.CallbackContext context)
        {
            // Debug.Log("Move!");
            _velocity = context.action.ReadValue<Vector2>();
            _animator.SetFloat("Speed", _velocity.magnitude); 
        }

        public void Fire(InputAction.CallbackContext context)
        {
            // Debug.Log("Fire!");
            var fire = context.action.ReadValue<float>();
            if (fire > 0)
                _fire = true;
            else
                _fire = false;
        }

        private void Update()
        {

            // update health bar
            _healthBar.SetSize(_player.HealthNormalised);

            _enableInputTimer.UpdateAsCooldown(Time.deltaTime);
            _ignoreCollisionTimer.UpdateAsCooldown(Time.deltaTime);

            if (_ignoreCollisionTimer.IsTimeElapsed)
            {
                gameObject.layer = 8;               // restore us to player layer
                _childCollection.SetActive(true);   // show visual player objects
            }

            // Debug.Log($"[S] InputDelayTime '{_enableInputTimer.Time}'");

            if (!_enableInputTimer.IsTimeElapsed)
                return;

            if (_isInputDisabled)
                return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                _isFogOfWar = !_isFogOfWar;
                if (_isFogOfWar)
                    _enemySpriteMask.transform.localScale = new Vector3(3, 3, 1);
                else
                    _enemySpriteMask.transform.localScale = new Vector3(10, 10, 1);
            }

            // position our target
            var mouse = Mouse.current;
            var delta = mouse.delta.ReadValue();

            _targetPosition += delta * Time.deltaTime * _targetSpeed;

            if (_targetPosition.magnitude > _distanceOfSight)
                _targetPosition = _targetPosition.normalized * _distanceOfSight; // limit the range

            _target.transform.position = _targetPosition + (Vector2)transform.position;

            // work out player direction and set arrow accordingly
            _rotation = Mathf.Atan2(_targetPosition.y, _targetPosition.x) - 90 * Mathf.Deg2Rad;
            _sprite.transform.rotation = Quaternion.Euler(0f, 0f, _rotation * Mathf.Rad2Deg);


            // activate key
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RaycastHit2D hit = Physics2D.Raycast(_rigidbody2d.position, _lookDirection, 1.0f, LayerMask.GetMask("InteractObjects"));
                if (hit.collider != null)
                {
                    Debug.Log("Raycast has hit the object " + hit.collider.gameObject);

                    IObjectView thing = hit.collider.GetComponent<IObjectView>();
                    if (thing != null)
                    {
                        _player.SetActivate(thing.GetId(), thing.GetObjectType());
                    }
                }
            }

            // Form command packet from _velocity given by InputSystem notification
            _command = _player.SetInput(new WorldVector(_velocity.x, _velocity.y), _rotation, _fire);

            // update player position in command
            _player.SetCommandPosition(new WorldVector(transform.position.x, transform.position.y));

        }

        void FixedUpdate()
        {
            // calculate rotation vector of forward motion
            _lookDirection = _targetPosition.normalized;

            Vector2 up = new Vector2(0, -1);
            Vector2 down = new Vector2(0, 1);
            Vector2 left = new Vector2(-1, 0);
            Vector2 right = new Vector2(1, 0);

            Vector2 actualVelocity = Vector2.zero;

            if (_clientLogic.IsOldKeys)
            {
                // if you are confused as to why down is the look direction - it is because the arrow sprite is 180° out

                // calculate rotation vector of reverse motion
                down = _lookDirection;

                // calculate rotation vector of forward motion
                up = new Vector2(0 - _lookDirection.x, 0 - _lookDirection.y);

                // calculate rotation vector of left motion
                right = new Vector2(MathFloat.Cos(_rotation), MathFloat.Sin(_rotation));

                // calculate rotation vector of reverse motion
                left = new Vector2(0 - right.x, 0 - right.y);
            }

            // act on command set in Update()
            if ((_command.Keys & MovementKeys.Up) != 0)
                actualVelocity = up;
            if ((_command.Keys & MovementKeys.Down) != 0)
                actualVelocity = down;
            if ((_command.Keys & MovementKeys.Left) != 0)
                actualVelocity += left;
            if ((_command.Keys & MovementKeys.Right) != 0)
                actualVelocity += right;

            actualVelocity.Normalize();

            Vector2 newPosition;

            // calculate new position
            newPosition = (Vector2)transform.position + (actualVelocity * _player.Speed * Time.deltaTime) + _serverVelocity;

            // if a server velocity has been applied, zero out the value
            if (_serverVelocity.x != 0f || _serverVelocity.y != 0)
                _serverVelocity = Vector2.zero;

            // stop ridgid body moving
            // without these lines the player will keep moving when pushed by a monster
            // even after the monster is out of range
            _rigidbody2d.Sleep();
            _rigidbody2d.WakeUp();

            // Set ridgidbody (apply physical position)
            _rigidbody2d.transform.position = newPosition;

        }

        public void Destroy() 
        {
            Destroy(gameObject);
        }

        public void SetPositionCorrection(Vector2 delta)
        {
            _serverVelocity += delta;
        }

        public byte GetId()
        {
            return _player.Id;
        }

        public void PickupObject(int objectId)
        {
            _player.SetPickup(objectId);
        }

        public void DisableInput(bool isInputDisabled)
        {
            _isInputDisabled = isInputDisabled;
            Cursor.visible = isInputDisabled;
            if (!_isInputDisabled)
                _enableInputTimer.Reset();
        }

        public void Spawn(float x, float y)
        {
            _childCollection.SetActive(false);      // hide visual player objects
            gameObject.layer = 16;                  // move us to the ignore collision layer
            transform.position = new Vector2(x, y); // set new location
            _ignoreCollisionTimer.Reset();          // start the timer for restore
        }

        public void Hide(bool isHidden)
        {
            _fogOfWarCircle.SetActive(!isHidden);
        }

        void IPlayerView.SetActive(bool bActive)
        {
            gameObject.SetActive(bActive);
        }

        GameObject IPlayerView.GetGameObject()
        {
            return gameObject;
        }


    }
}