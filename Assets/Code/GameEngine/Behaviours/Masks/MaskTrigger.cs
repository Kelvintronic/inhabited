using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;
using UnityEngine.Tilemaps;

public class MaskTrigger : MonoBehaviour
{
    [SerializeField] private Tilemap _maskMap;

    [SerializeField] private Tilemap _pointMap;

    private ServerLogic _serverLogic;

    // Start is called before the first frame update
    void Start()
    {
        _serverLogic=FindObjectOfType<ServerLogic>();
        _pointMap.gameObject.SetActive(false); // hide delete points
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // trying a new format to avoid nested indentation, exit as soon as possible, otherwise continue
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        this.gameObject.SetActive(false);
        _maskMap.gameObject.SetActive(false);

        var deletePoints = GetDeletePoints();

        // if we are hosing
        if (_serverLogic.IsStarted)
        {
            // iterate through deletePoints
            foreach (var point in deletePoints)
            {
                Debug.Log("Delete Point: x=" + point.x + " y=" + point.y);

                _serverLogic.OnTriggerDeletePoint(point);
            }
        }
    }

    private List<WorldVector> GetDeletePoints()
    {
        Vector2 mapOrigin = _pointMap.transform.position;

        var pointList = new List<WorldVector>();

        Vector3Int cellPosition = new Vector3Int();
        for (int y = _pointMap.cellBounds.yMin; y <= _pointMap.cellBounds.yMax; y++)
            for (int x = _pointMap.cellBounds.xMin; x <= _pointMap.cellBounds.xMax; x++)
            {
                cellPosition.x = x;
                cellPosition.y = y;
                Tile tile = _pointMap.GetTile<Tile>(cellPosition);
                if (tile == null)
                    continue;

                switch (tile.sprite.name)
                {
                    case "LayoutSprites_13": // D
                        pointList.Add(new WorldVector(cellPosition.x + mapOrigin.x + 0.5f, cellPosition.y + mapOrigin.y + 0.5f));
                        break;
                }
            }

        return pointList;
    }


}
