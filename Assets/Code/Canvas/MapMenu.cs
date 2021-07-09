using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Cinemachine;
using GameEngine;
public struct TileKey
{
    public TileBase tile;
    public char key;
}

public class MapMenu : MonoBehaviour
{

    public Dropdown algorithmDrop;
    public Slider cameraZoomSlider;
    public Slider fillSlider;
    public Text fillValue;

    public CinemachineVirtualCamera camera;

    [SerializeField] private LevelSet levelSet;
    [SerializeField] private Tilemap sourceTiles;

    private Level testLevel;

    private LevelGenerator levelGen;
    private Algorithm algorithm;

    private Sprite[] _layoutSpriteArray;
    private Tile[] _layoutTileArray = new Tile[20];
    private Sprite[] _dandySpriteArray;
    private Tile[] _dandyTileArray = new Tile[8];
    private Sprite[] _dandyObjectSpriteArray;
    private Tile[] _dandyObjectTileArray = new Tile[17];

    private Vector2 _velocity;


    private List<string> algorithmList = new List<string>() { "Perlin", "PerlinSmoothed", "PerlinCave", "RandomWalkTop", "RandomWalkTopSmoothed", "RandomWalkCave", "RandomWalkCaveCustom", "CellularAutomataVonNeuman", "CellularAutomataMoore", "DirectionalTunnel" };

    private void Awake()
    {
        algorithmDrop.ClearOptions();
        algorithmDrop.AddOptions(algorithmList);

        _layoutSpriteArray = Resources.LoadAll<Sprite>("LayoutSprites");
        _dandySpriteArray = Resources.LoadAll<Sprite>("DandySprites");
        _dandyObjectSpriteArray = Resources.LoadAll<Sprite>("DandyObjectSprites");
        for (int i = 0; i < 20; i++)
        {
            _layoutTileArray[i] = ScriptableObject.CreateInstance<Tile>();
            _layoutTileArray[i].sprite = _layoutSpriteArray[i];
            if(i<17)
            {
                _dandyObjectTileArray[i] = ScriptableObject.CreateInstance<Tile>();
                _dandyObjectTileArray[i].sprite = _dandyObjectSpriteArray[i];
            }
            if (i<8)
            {
                _dandyTileArray[i] = ScriptableObject.CreateInstance<Tile>();
                _dandyTileArray[i].sprite = _dandySpriteArray[i];
            }
        }

        levelGen = new LevelGenerator(100, 60);

        // set defaults
        algorithmDrop.value = (int)Algorithm.CellularAutomataMoore;
        fillSlider.value = 47;

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void Move(InputAction.CallbackContext context)
    {
        // Debug.Log("Move Map!");
        _velocity = context.action.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        var velocity = _velocity * Time.deltaTime * 10;
        camera.transform.position = new Vector3(camera.transform.position.x + velocity.x, camera.transform.position.y + velocity.y, -10f);
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);

        levelSet.EnableCustomLevel(isVisible);

        if (isVisible)
            testLevel = levelSet.GetCurrentLevel();
    }

    public void OnChangeAlgorithm()
    {
        algorithm = (Algorithm)algorithmDrop.value;

        fillSlider.gameObject.SetActive(false);

        switch (algorithm)
        {
            case Algorithm.Perlin:

                break;
            case Algorithm.PerlinSmoothed:

                break;
            case Algorithm.PerlinCave:

                break;
            case Algorithm.RandomWalkTop:

                break;
            case Algorithm.RandomWalkTopSmoothed:

                break;
            case Algorithm.RandomWalkCave:

                break;
            case Algorithm.RandomWalkCaveCustom:
                    
                break;
            case Algorithm.CellularAutomataVonNeuman:
                fillSlider.gameObject.SetActive(true);
                break;
            case Algorithm.CellularAutomataMoore:
                fillSlider.gameObject.SetActive(true);
                break;
            case Algorithm.DirectionalTunnel:

                break;
        }
    }

    public void OnChangeZoom()
    {
        camera.m_Lens.OrthographicSize = cameraZoomSlider.value;
    }

    public void OnChangeFill()
    {
        levelGen.fill = Convert.ToInt32(fillSlider.value);
        fillValue.text = levelGen.fill.ToString();
    }

    public void OnGenerateMap()
    {
        var map = levelGen.GenerateMap(algorithm);

        //Render the result
        testLevel.RenderCustomMap(map,_dandyTileArray[0]);

        // fill the caves
        var caves = MapHelpers.FillCaves(map);

        // find the largest cave
        int caveSize = 0;
        MapCave largestCave = null;
        foreach( var cave in caves)
        {
            if(cave.size>caveSize)
            {
                caveSize = cave.size;
                largestCave = cave;
            }
        }

        // find a random spot on the edge of the cave to place the U
        if(largestCave!=null)
        {
            int offsetx = map.GetUpperBound(0) / 2;
            int offsety = map.GetUpperBound(1) / 2;

            Debug.Log("Largest cave is " + largestCave.size + " cells big.");

            //testLevel.GetTilemap().SetTile(new Vector3Int(largestCave.x - offsetx, largestCave.y - offsety, 0), _dandyTileArray[3]);

            // clear the cave of it's code (for now) refactor required
            MapHelpers.FloodFill(map, largestCave.x, largestCave.y, 0);

            // Trace the cave wall and populate MapCave::borderCells
            MapHelpers.TraceCaveWall(map, largestCave);

            // find a random point on the border
            int spawnIndex = UnityEngine.Random.Range(0, largestCave.borderCells.Count - 1);

            var newSpawnPoint = MapHelpers.BufferPoint(map, largestCave.borderCells[spawnIndex], 1, 1);

            // Bufferpoint is not perfect so clear tiles around new spawn point
            var emptyTile = ScriptableObject.CreateInstance<Tile>();
            for (int x=-1; x<2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    testLevel.GetTilemap().SetTile(new Vector3Int(newSpawnPoint.x - offsetx+x, newSpawnPoint.y - offsety+y, 0), null);
                }
            }

            testLevel.MoveSpawnPoint(new Vector2Int(newSpawnPoint.x-offsetx,newSpawnPoint.y - offsety));

            testLevel.ResetArray();
        }


    }

}

public enum Algorithm
{
    Perlin, PerlinSmoothed, PerlinCave, RandomWalkTop, RandomWalkTopSmoothed, RandomWalkCave, RandomWalkCaveCustom, CellularAutomataVonNeuman, CellularAutomataMoore, DirectionalTunnel
}
public class LevelGenerator
{
  //  private Tilemap tilemap;
  //  private TileBase[] sourceTiles;
    private int width;
    private int height;

    public int fill = 20;
    public LevelGenerator(int _width, int _height) //, Tilemap _tilemap, TileBase[] _sourceTiles)
    {
       // tilemap = _tilemap;
        width = _width;
        height = _height;
     //   sourceTiles = _sourceTiles;
    }

 /*   public void SetTileMap(Tilemap _tilemap)
    {
        tilemap = _tilemap;
    }*/

    public int [,] GenerateMap(Algorithm algorithm)
    {
        return GenerateMap(Time.time, algorithm);
    }
    public int[,] GenerateMap(float seed, Algorithm algorithm)
    {
        int[,] map = new int[width, height];

        //Generate the map depending the algorithm selected
        switch (algorithm)
        {
            case Algorithm.Perlin:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, true);
                //Next generate the perlin noise onto the array
                map = MapFunctions.PerlinNoise(map, seed);
                break;
            case Algorithm.PerlinSmoothed:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, true);
                //Next generate the perlin noise onto the array
                // mapLayer.interval = EditorGUILayout.IntSlider("Interval Of Points", mapLayer.interval, 1, 10);
                map = MapFunctions.PerlinNoiseSmooth(map, seed, 5);
                break;
            case Algorithm.PerlinCave:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, true);
                //Next generate the perlin noise onto the array
                // mapLayer.edgesAreWalls = EditorGUILayout.Toggle("Edges Are Walls", mapLayer.edgesAreWalls);
                // mapLayer.modifier = EditorGUILayout.Slider("Modifier", mapLayer.modifier, 0.0001f, 1.0f);
                map = MapFunctions.PerlinNoiseCave(map, 0.01f, true);
                break;
            case Algorithm.RandomWalkTop:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, true);
                //Next generater the random top
                map = MapFunctions.RandomWalkTop(map, seed);
                break;
            case Algorithm.RandomWalkTopSmoothed:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, true);
                //Next generate the smoothed random top
                // mapLayer.interval = EditorGUILayout.IntSlider("Minimum Section Length", mapLayer.interval, 1, 10);
                map = MapFunctions.RandomWalkTopSmoothed(map, seed, 5);
                break;
            case Algorithm.RandomWalkCave:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, false);
                //Next generate the random walk cave
                // mapLayer.clearAmount = EditorGUILayout.IntSlider("Amount To Clear", mapLayer.clearAmount, 0, 100);
                map = MapFunctions.RandomWalkCave(map, seed, 10);
                break;
            case Algorithm.RandomWalkCaveCustom:
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, false);
                //Next generate the custom random walk cave
                int clearAmount = 10; // EditorGUILayout.IntSlider("Amount To Clear", mapLayer.clearAmount, 0, 100);
                map = MapFunctions.RandomWalkCaveCustom(map, seed, clearAmount);
                break;
            case Algorithm.CellularAutomataVonNeuman:
                //First generate the cellular automata array
                //mapLayer.edgesAreWalls = EditorGUILayout.Toggle("Edges Are Walls", mapLayer.edgesAreWalls);
                //mapLayer.fillAmount = EditorGUILayout.IntSlider("Fill Percentage", mapLayer.fillAmount, 0, 100);
                int smoothAmount = 10; //= EditorGUILayout.IntSlider("Smooth Amount", mapLayer.smoothAmount, 0, 10);
                map = MapFunctions.GenerateCellularAutomata(width, height, seed, fill, true);
                //Next smooth out the array using the von neumann rules
                map = MapFunctions.SmoothVNCellularAutomata(map, true, smoothAmount);
                break;
            case Algorithm.CellularAutomataMoore:
                // mapLayer.edgesAreWalls = EditorGUILayout.Toggle("Edges Are Walls", mapLayer.edgesAreWalls);
                // mapLayer.fillAmount = EditorGUILayout.IntSlider("Fill Percentage", mapLayer.fillAmount, 0, 100);
                // mapLayer.smoothAmount = EditorGUILayout.IntSlider("Smooth Amount", mapLayer.smoothAmount, 0, 10);
                //First generate the cellular automata array
                map = MapFunctions.GenerateCellularAutomata(width, height, seed, fill, true);
                //Next smooth out the array using the Moore rules
                map = MapFunctions.SmoothMooreCellularAutomata(map, true, 10);
                break;
            case Algorithm.DirectionalTunnel:
                int minPathWidth = 5; // EditorGUILayout.IntField("Minimum Path Width", mapLayer.minPathWidth);
                int maxPathWidth = 20; // EditorGUILayout.IntField("Maximum Path Width", mapLayer.maxPathWidth);
                int maxPathChange = 20; // EditorGUILayout.IntField("Maximum Path Change", mapLayer.maxPathChange);
                int windyness = 10; // EditorGUILayout.IntSlider(new GUIContent("Windyness", "This is checked against a random number to determine if we can change the paths current x position"), mapLayer.windyness, 0, 100);
                int roughness = 10; // EditorGUILayout.IntSlider(new GUIContent("Roughness", "This is checked against a random number to determine if we can change the width of the tunnel"), mapLayer.roughness, 0, 100);
                //First generate our array
                map = MapFunctions.GenerateArray(width, height, false);
                //Next generate the tunnel through the array
                map = MapFunctions.DirectionalTunnel(map, minPathWidth, maxPathWidth, maxPathChange, roughness, windyness);
                break;
        }

        return map;

    }

}
