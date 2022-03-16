using UnityEngine;

namespace GameEngine
{
    public class RemotePlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private PlayerSpriteSet _sprites;
        [SerializeField] private GameObject _serverProjectilePrefab;
        [SerializeField] private GameObject _clientProjectilePrefab;
        [SerializeField] private TextMesh _handleText;

        private Animator _animator;
        private PlayerColour _colour;
        private GameTimer _animationRefreshTimer;
        private HealthBar _healthBar;
        private RemotePlayer _player;

        public static RemotePlayerView Create(RemotePlayerView prefab, RemotePlayer player, PlayerColour colour = PlayerColour.Red)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, new Vector2(player.Position.x,player.Position.y), rot);
            obj._player = player;
            obj._colour = colour;
            return obj;
        }

        public WorldVector GetActualPosition()
        {
            return new WorldVector(transform.position.x, transform.position.y);
        }

        private void Start()
        {
            _sprites.SetSprite(_colour);
            _animator = _sprites.GetAnimator();
            _animationRefreshTimer = new GameTimer(1.0f);
            _healthBar = GetComponentInChildren<HealthBar>();
            _handleText.text = _player.Name;
        }

        private void Update()
        {
            _animationRefreshTimer.UpdateAsCooldown(Time.deltaTime);
            if (_animator != null)
            {
                if (_player.IsMoving)
                {
                    _animator.SetFloat("Speed", 1);
                    _animationRefreshTimer.Reset();
                }
                else
                {
                    // delay stopping animation
                    if (_animationRefreshTimer.IsTimeElapsed)
                        _animator.SetFloat("Speed", 0);
                }
            }
            _player.UpdatePosition(Time.deltaTime);
            transform.position = new Vector2(_player.Position.x, _player.Position.y);
            _sprites.transform.rotation =  Quaternion.Euler(0f, 0f, _player.Rotation * Mathf.Rad2Deg );


            // update health bar
            _healthBar.SetSize(_player.HealthNormalised);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        void IPlayerView.SetActive(bool bActive)
        {
            gameObject.SetActive(bActive);
        }

        public byte GetId()
        {
            return _player.Id;
        }

        GameObject IPlayerView.GetGameObject()
        {
            return gameObject;
        }

    }
}