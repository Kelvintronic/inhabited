using UnityEngine;

namespace GameEngine
{
    public class RemotePlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private GameObject _arrow;
        [SerializeField] private GameObject _serverProjectilePrefab;
        [SerializeField] private GameObject _clientProjectilePrefab;
        [SerializeField] private TextMesh _handleText;


        private HealthBar _healthBar;
        private RemotePlayer _player;

        public static RemotePlayerView Create(RemotePlayerView prefab, RemotePlayer player, Sprite sprite = null)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, new Vector2(player.Position.x,player.Position.y), rot);
            obj._player = player;
            if (sprite != null)
                obj.SetSprite(sprite);
            return obj;
        }

        public void SetSprite(Sprite sprite)
        {
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
        }

        public WorldVector GetActualPosition()
        {
            return new WorldVector(transform.position.x, transform.position.y);
        }

        private void Start()
        {
            _healthBar = GetComponentInChildren<HealthBar>();
            _handleText.text = _player.Name;
        }

        private void Update()
        {
            _player.UpdatePosition(Time.deltaTime);
            transform.position = new Vector2(_player.Position.x, _player.Position.y);
            _arrow.transform.rotation =  Quaternion.Euler(0f, 0f, _player.Rotation * Mathf.Rad2Deg );

            // update health bar
            _healthBar.SetSize(_player.HealthNormalised);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public GameObject Shoot(bool isServer)
        {
            var shotSpawnRot = _arrow.transform.rotation; 
            var shotSpawnPos = _arrow.transform.position + (shotSpawnRot * Vector3.up);

            GetComponent<AudioSource>().Play();

            if(isServer)
                return Instantiate(_serverProjectilePrefab, shotSpawnPos, shotSpawnRot);
            else
                return Instantiate(_clientProjectilePrefab, shotSpawnPos, shotSpawnRot);

        }

        void IPlayerView.SetActive(bool bActive)
        {
            gameObject.SetActive(bActive);
        }

        public byte GetId()
        {
            return _player.Id;
        }
    }
}