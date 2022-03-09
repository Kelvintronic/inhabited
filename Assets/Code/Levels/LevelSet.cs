using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class LevelSet : MonoBehaviour
    {
        public Tilemap sourceTiles;
        [SerializeField] private Level _lobby;
        [SerializeField] private List<Level> _levelPrefabs;
        [SerializeField] private Level _customLevel;
        public Camera mapCamera;

        private bool _isLobby = true;
        private bool _isCustomLevel = false;
        private Level _currentLevel;

        private ClientLogic _clientLogic;
        private ServerLogic _serverLogic;
        
        private ushort _nMap;

        private Sprite[] _dandySpriteArray;
        private Tile[] _dandyTileArray = new Tile[8];

        public bool IsCustomLevel => _isCustomLevel && _nMap==0;
        public ushort CurrentLevelIndex => _nMap;
        public int NumberOfLevels => _levelPrefabs.Count + 1;

        private void Awake()
        {
            _clientLogic = GameObject.FindObjectOfType<ClientLogic>();
            _serverLogic = GameObject.FindObjectOfType<ServerLogic>();
            _currentLevel = _lobby;

            _dandySpriteArray = Resources.LoadAll<Sprite>("DandySprites");
            for (int i = 0; i < 8; i++)
            {
                _dandyTileArray[i] = ScriptableObject.CreateInstance<Tile>();
                _dandyTileArray[i].sprite = _dandySpriteArray[i];
            }
        }

        public ushort GetMap()
        {
            return _nMap;
        }

        public Level GetCurrentLevel()
        {
            return _currentLevel;
        }

        public void EnableCustomLevel(bool isCustomLevel)
        {
            _isCustomLevel = isCustomLevel;
            SetMap(0);
        }

        public bool SetMap(ushort nMap)
        {
            // note: actual maps start from 1 (because lobby is always 0)
            if (nMap > _levelPrefabs.Count)
                return false;

            // disable or destroy current level
            if(_currentLevel!=null)
            {
                if (_isLobby)
                    _currentLevel.gameObject.SetActive(false);
                else
                    Destroy(_currentLevel.gameObject);
            }

            if (nMap > 0)
            {
                _isLobby = false;
                _currentLevel = Level.Create(_levelPrefabs[nMap-1]);
            }
            else
            {
                _isLobby = true;
                _lobby.ResetArray();
                if (_isCustomLevel)
                    _currentLevel = _customLevel;
                else
                    _currentLevel = _lobby;
            }

            _currentLevel.gameObject.SetActive(true);
            _nMap = nMap;

            // set mapCamera to position and size of new map
            var mapCenter = _currentLevel.GetMapCenter();
            mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, -10f);

            float orthoScreenSize = 0.5f * Screen.height / Screen.width;
            Debug.Log($"width= {Screen.width}");
            Debug.Log($"height= {Screen.height}");
            Debug.Log($"ortho= {orthoScreenSize}");

            if (_currentLevel.GetMapHeight()>20)
            {
                if (_currentLevel.GetMapHeight() < _currentLevel.GetMapWidth())
                    mapCamera.orthographicSize = (_currentLevel.GetMapWidth() + 4) * orthoScreenSize;
                else
                    mapCamera.orthographicSize = _currentLevel.GetMapHeight() / 2;
            }
            else
                mapCamera.orthographicSize = 20;

            Debug.Log($"ortho set to= {mapCamera.orthographicSize}");




            return true;
        }

        public bool SetCustomMap(MapArray map, WorldVector spawnPoint)
        {
            _customLevel.RenderCustomMap(map, _dandyTileArray[0]);
            _customLevel.MoveSpawnPoint(new Vector2(spawnPoint.x, spawnPoint.y));
            _isCustomLevel = true;
            SetMap(0);
            return true;
        }

        public Vector2 GetSpawnPoint(int nPlayer)
        {
            Vector2 spawnPoint;
            spawnPoint = _currentLevel.GetSpawnPoint();

            switch(nPlayer)
            {
                case 0:
                    return spawnPoint + Vector2.up;
                case 1:
                    return spawnPoint + Vector2.right;
                case 2:
                    return spawnPoint + Vector2.down;
                case 3:
                    return spawnPoint + Vector2.left;
            }
            return spawnPoint;
        }
    }
}
