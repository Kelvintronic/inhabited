using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using GameEngine.Search;

namespace GameEngine
{

    public class Level : MonoBehaviour
    {
        [SerializeField] private SpawnPoint spawnPoint;
        [SerializeField] private Tilemap mainMap;
        [SerializeField] private Tilemap objectMap;
        [SerializeField] private Tilemap debugMap;
        [SerializeField] private GameObject[] serverChildren;

        private Sprite[] _layoutSpriteArray;
        private Tile[] _layoutTileArray = new Tile[20];

        private MapArray _objectArray;
        private MapArray _mainArray;
        private MapArray _mainArrayCopy;

        private float _xMax;
        private float _xMin;
        private float _yMax;
        private float _yMin;

       // private ClientLogic _clientLogic;
        private ServerLogic _serverLogic;

        private bool _debugVisible = false;
        public bool IsDebugVisible => _debugVisible;

        private void Awake()
        {
         //   _clientLogic = GameObject.FindObjectOfType<ClientLogic>();
            _serverLogic = GameObject.FindObjectOfType<ServerLogic>();
            objectMap.gameObject.SetActive(false); // hide the object layer
            _layoutSpriteArray = Resources.LoadAll<Sprite>("LayoutSprites");

            for (int i = 0; i < 20; i++)
            {
                _layoutTileArray[i] = ScriptableObject.CreateInstance<Tile>();
                _layoutTileArray[i].sprite = _layoutSpriteArray[i];
            }

            // create the map array now to populate variables for map bounds
            GetMapArray();

        }

        // Start is called before the first frame update
        void Start()
        {
            // if we have not been created by the server, disable all server children
            // server children are prefabs placed within a level that should only be active
            // in the server instance. Generally these prefabs send notifications locally to
            // the serverlogic
       /*     if(serverChildren.Length>0 && !_serverLogic.IsStarted)
            {
                for(int i=0; i < serverChildren.Length; i++)
                {
                    serverChildren[i].SetActive(false);
                }
            }*/
        }

        public static Level Create(Level prefab)
        {
            return Instantiate(prefab, Vector2.zero, Quaternion.identity);
        }

        public Tilemap GetTilemap()
        {
            return mainMap;
        }

        public Tilemap GetObjectMap()
        {
            return objectMap;
        }

        public void MoveSpawnPoint(Vector2Int newPoint)
        {
            spawnPoint.gameObject.transform.position = new Vector3(newPoint.x+0.5f, newPoint.y+0.5f, 0);
        }
        public void MoveSpawnPoint(Vector2 newPoint)
        {
            spawnPoint.gameObject.transform.position = new Vector3(newPoint.x, newPoint.y, 0);
        }

        public Vector2 GetMapCenter()
        {
            float xCenter = (_xMax - _xMin) / 2 + _xMin;
            float yCenter = (_yMax - _yMin) / 2 + _yMin;
            return new Vector2(xCenter, yCenter);
        }

        public int GetMapHeight()
        {
            return (int)(_yMax - _yMin);
        }
        public int GetMapWidth()
        {
            return (int)(_xMax - _xMin);
        }

        public bool ShowDebugMap(bool isVisible)
        {
            _debugVisible = isVisible;
            debugMap.gameObject.SetActive(isVisible);
            return _debugVisible;
        }

        public void SetDebugMap(MapArray debugArray)
        {
            debugMap.ClearAllTiles();

            Vector3Int cellPosition = new Vector3Int();
            for (int y = 0; y < debugArray.yCount; y++)
                for (int x = 0; x < debugArray.xCount; x++)
                {
                    cellPosition.x = x + debugArray.xOffset;
                    cellPosition.y = y + debugArray.yOffset;

                    int tileIndex = 1;
                    switch (debugArray.Array[x,y].type)
                    {
                        case ObjectType.None:
                            // Empty cell for none
                            tileIndex = 0;
                            break;
                    }
                    debugMap.SetTile(cellPosition, _layoutTileArray[tileIndex]);
                }
        }

        /// <summary>
        /// Draws the map to the screen
        /// </summary>
        /// <param name="map">Map that we want to draw</param>
        /// <param name="tilemap">Tilemap we will draw onto</param>
        /// <param name="tile">Tile we will draw with</param>
        public void RenderCustomMap(int[,] map, TileBase tile)
        {
            ResetArray();
            mainMap.ClearAllTiles(); //Clear the map (ensures we dont overlap)

            int offsetx = map.GetUpperBound(0) / 2;
            int offsety = map.GetUpperBound(1) / 2;

            for (int x = 0; x < map.GetUpperBound(0); x++) //Loop through the width of the map
            {
                for (int y = 0; y < map.GetUpperBound(1); y++) //Loop through the height of the map
                {
                    if (map[x, y] == 1)
                        mainMap.SetTile(new Vector3Int(x - offsetx, y - offsety, 0), tile);
                }
            }
        }

        public void RenderCustomMap(MapArray map, TileBase tile)
        {
            mainMap.ClearAllTiles(); //Clear the map (ensures we dont overlap)

            int offsetx = map.Array.GetUpperBound(0) / 2;
            int offsety = map.Array.GetUpperBound(1) / 2;

            for (int x = 0; x < map.Array.GetUpperBound(0); x++) //Loop through the width of the map
            {
                for (int y = 0; y < map.Array.GetUpperBound(1); y++) //Loop through the height of the map
                {
                    if (map.Array[x, y].type == ObjectType.Wall)
                        mainMap.SetTile(new Vector3Int(x - offsetx, y - offsety, 0), tile);
                }
            }
            CreateMapArray();
        }

        public void ResetArray()
        {
            CreateMapArray();
            if (_objectArray != null)
                _objectArray.ClearExceptions(); // required to allow external calculation of mass objects (e.g. doors)

        }

        public MapArray GetMapArray()
        {
            if (_mainArray != null)
                return _mainArray;

            return CreateMapArray();
        }

        private MapArray CreateMapArray()
        { 
            _mainArray = new MapArray(mainMap.cellBounds.xMax - mainMap.cellBounds.xMin+1,
                                       mainMap.cellBounds.yMax - mainMap.cellBounds.yMin+1,
                                       mainMap.cellBounds.xMin,
                                       mainMap.cellBounds.yMin);

            // variable to help find the dimensions of the map
            // we do this because the bound properties of a TileMap may include empty tiles outside the map external walls
            float xCenter = (mainMap.cellBounds.xMax - mainMap.cellBounds.xMin) / 2;
            float yCenter = (mainMap.cellBounds.yMax - mainMap.cellBounds.yMin) / 2;

            _xMin = xCenter;
            _yMin = yCenter;
            _xMax = xCenter;
            _yMax = yCenter;


            Vector3Int cellPosition = new Vector3Int();
            for (int y = 0; y < _mainArray.yCount; y++)
                for (int x = 0; x < _mainArray.xCount; x++)
                {
                    cellPosition.x = x+ _mainArray.xOffset;
                    cellPosition.y = y+ _mainArray.yOffset;
                    Tile tile = mainMap.GetTile<Tile>(cellPosition);
                    _mainArray.Array[x, y].type = ObjectType.None;
                    _mainArray.Array[x, y].id = -1;
                    if (tile == null)
                        continue;

                    if(cellPosition.x==0||cellPosition.y==0)
                    {
                        //Debug.Log($"[S] Found wall at:'{cellPosition.x}','{cellPosition.y}'");
                    }
                    switch (tile.sprite.name)
                    {
                        case "DandySprites_0": // wall
                            _mainArray.Array[x, y].type = ObjectType.Wall;
                            if (x < _xMin)
                                _xMin = x;
                            if (x > _xMax)
                                _xMax = x;
                            if (y < _yMin)
                                _yMin = y;
                            if (y > _yMax)
                                _yMax = y;
                            break;
                    }
                }

            _xMin += _mainArray.xOffset;
            _xMax += _mainArray.xOffset;
            _yMin += _mainArray.yOffset;
            _yMax += _mainArray.yOffset;

            return _mainArray;
        }

        public MapArray GetObjectArray()
        {
            if (_objectArray != null)
                return _objectArray;

            _objectArray = new MapArray(objectMap.cellBounds.xMax - objectMap.cellBounds.xMin+1,
                                       objectMap.cellBounds.yMax - objectMap.cellBounds.yMin+1,
                                       objectMap.cellBounds.xMin,
                                       objectMap.cellBounds.yMin);

            Vector3Int cellPosition = new Vector3Int();
            for (int y = 0; y < _objectArray.yCount; y++)
                for (int x = 0; x < _objectArray.xCount; x++)
                {
                    cellPosition.x = x + _objectArray.xOffset;
                    cellPosition.y = y + _objectArray.yOffset;
                    Tile tile = objectMap.GetTile<Tile>(cellPosition);
                    _objectArray.Array[x, y].type = ObjectType.None;
                    _objectArray.Array[x, y].id = -1;
                    if (tile == null)
                        continue;
                    if (cellPosition.x == 0 || cellPosition.y == 0)
                    {
                        Debug.Log($"[S] Found object at:'{cellPosition.x}','{cellPosition.y}'");
                    }
                    switch (tile.sprite.name)
                    {
                        case "ObjectSprites_0": // red key
                            _objectArray.Array[x, y].type = ObjectType.KeyRed;
                            break;
                        case "ObjectSprites_1": // green key
                            _objectArray.Array[x, y].type = ObjectType.KeyGreen;
                            break;
                        case "ObjectSprites_2": // blue key
                            _objectArray.Array[x, y].type = ObjectType.KeyBlue;
                            break;
                        case "ObjectSprites_3": // bomb
                            _objectArray.Array[x, y].type = ObjectType.Bomb;
                            break;
                        case "ObjectSprites_4": // heart
                            _objectArray.Array[x, y].type = ObjectType.Heart;
                            break;
                        case "ObjectSprites_5": // red door
                            _objectArray.Array[x, y].type = ObjectType.DoorRed;
                            break;
                        case "ObjectSprites_6": // green door
                            _objectArray.Array[x, y].type = ObjectType.DoorGreen;
                            break;
                        case "ObjectSprites_7": // blue door
                            _objectArray.Array[x, y].type = ObjectType.DoorBlue;
                            break;
                        case "ObjectSprites_8": // health
                            _objectArray.Array[x, y].type = ObjectType.Health;
                            break;
                        case "ObjectSprites_9": // cash
                            _objectArray.Array[x, y].type = ObjectType.Cash;
                            break;
                        case "ObjectSprites_16": // false wall
                            _objectArray.Array[x, y].type = ObjectType.FalseWall;
                            break;
                        case "ObjectSprites_17": // hidden door
                            _objectArray.Array[x, y].type = ObjectType.HiddenDoor;
                            break;
                        case "ObjectSprites_18": // normal door
                            _objectArray.Array[x, y].type = ObjectType.Door;
                            break;
                        case "ObjectSprites_19": // barricade
                            _objectArray.Array[x, y].type = ObjectType.Barricade;
                            break;

                        // NPC and Generators
                        case "ObjectSprites_10":
                        case "ObjectSprites_11":
                        case "ObjectSprites_12":
                            _objectArray.Array[x, y].type = ObjectType.BugNest;
                            break;
                        case "ObjectSprites_13":
                            _objectArray.Array[x, y].type = ObjectType.NPCBug;
                            break;
                        case "ObjectSprites_14":
                            _objectArray.Array[x, y].type = ObjectType.NPCTrader;
                            break;
                        case "ObjectSprites_15":
                            _objectArray.Array[x, y].type = ObjectType.NPCMercenary;
                            break;


                        // Functional Layer
                        case "ArrowTiles_0":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 0;
                            break;
                        case "ArrowTiles_1":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 315;
                            break;
                        case "ArrowTiles_2":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 270;
                            break;
                        case "ArrowTiles_3":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 225;
                            break;
                        case "ArrowTiles_4":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 180;
                            break;
                        case "ArrowTiles_5":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 135;
                            break;
                        case "ArrowTiles_6":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 90;
                            break;
                        case "ArrowTiles_7":
                            _objectArray.Array[x, y].type = ObjectType.Conveyor;
                            _objectArray.Array[x, y].data = 45;
                            break;

                        // Exit Points
                        case "EnterExitTiles_1":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 0;
                            break;
                        case "EnterExitTiles_2":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 1;
                            break;
                        case "EnterExitTiles_3":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 2;
                            break;
                        case "EnterExitTiles_4":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 3;
                            break;
                        case "EnterExitTiles_5":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 4;
                            break;
                        case "EnterExitTiles_6":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 5;
                            break;
                        case "EnterExitTiles_7":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 6;
                            break;
                        case "EnterExitTiles_8":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 7;
                            break;
                        case "EnterExitTiles_9":
                            _objectArray.Array[x, y].type = ObjectType.ExitPoint;
                            _objectArray.Array[x, y].data = 8;
                            break;


                    }
                }
            return _objectArray;
        }

        public Vector2 GetSpawnPoint()
        {
            return spawnPoint.gameObject.transform.position;
        }
    }

    public struct MapCell
    {
        public ObjectType type;
        public int data;
        public int id;

        public static MapCell Empty { get { return new MapCell { type = ObjectType.None, data = 0, id = -1 }; } }
    }
    public class MapArray
    {
        public MapCell[,] Array;
        public int xCount;
        public int yCount;
        public int xOffset;
        public int yOffset;

        // variable used when finding common cells
        // used to exclude counting cells already included in another count
        // don't forget to clear this list if your reanalyse the array
        // using ClearExceptions()
        private List<Vector2Int> _exceptions = new List<Vector2Int>();

        public MapArray(int xCount,int yCount,int xOffset,int yOffset)
        {
            Array = new MapCell[xCount,yCount];
            this.xCount = xCount;
            this.yCount = yCount;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
        }

        public MapArray Copy()
        {
            MapArray newArray = new MapArray(xCount, yCount, xOffset, yOffset);
            for(int x=0; x<xCount;x++)
            {
                for(int y=0; y<yCount; y++)
                {
                    newArray.Array[x, y] = new MapCell { data = Array[x, y].data, id = Array[x, y].id, type = Array[x, y].type };
                }
            }
            return newArray;
        }

        public void SetCell(Vector2Int location, MapCell content)
        {
            Array[location.x, location.y].id = content.id;
            Array[location.x, location.y].type = content.type;
            Array[location.x, location.y].data = content.data;

        }

        public MapCell GetCell(WorldVector worldVector)
        {
            var cellPos = GetCellVector(worldVector);
            return Array[cellPos.x, cellPos.y];
        }

        public Vector2Int GetCellVector(WorldVector worldVector)
        {
            float x = worldVector.x -xOffset;
            float y = worldVector.y -yOffset;

            Vector2Int vector = new Vector2Int((int)Math.Floor(x),(int)Math.Floor(y));
            if (vector.x < 0 || vector.x > xCount || vector.y < 0 || vector.y > yCount)
                throw new ArgumentOutOfRangeException();
            return vector;
        }

        public WorldVector GetWorldVector(int x, int y)
        {
            WorldVector worldVector;
            worldVector.x = x + xOffset+0.5f;
            worldVector.y = y + yOffset+0.5f;
            return worldVector;
        }

        public void ClearExceptions()
        {
            _exceptions.Clear();
        }

        public bool IsException(int x, int y)
        {
            if (_exceptions.Exists(item => item.x == x && item.y == y))
                return true;

            return false;
        }

        public int CountCommonCells(int x, int y, bool isHorizontal = true)
        {
            if (x > xCount || y > yCount || x < 0 || y < 0)
                return 0;
            ObjectType cellContent = Array[x, y].type;
            int count = 0;
            if(isHorizontal)
            {
                while(x<xCount)
                {
                    if (Array[x, y].type == cellContent)
                    {
                        _exceptions.Add(new Vector2Int(x, y));
                        count++;
                    }
                    else
                        break;
                    x++;
                }
            }
            else
            {
                while(y<yCount)
                {
                    if (Array[x, y].type == cellContent)
                    {
                        _exceptions.Add(new Vector2Int(x, y));
                        count++;
                    }
                    else
                        break;
                    y++;
                }
            }
            return count;
        }
    }
}
