using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class MaskTrigger : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;

    public List<GameObject> deletePoints;

    private ServerLogic _serverLogic;

    // Start is called before the first frame update
    void Start()
    {
        _serverLogic=FindObjectOfType<ServerLogic>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // trying a new format to avoid nested indentation, exit as soon as possible, otherwise continue
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        this.gameObject.SetActive(false);
        _renderer.gameObject.SetActive(false);

        // if we are hosing
        if (_serverLogic.IsStarted)
        {
            // iterate through deletePoints
            foreach (var point in deletePoints)
            {
                _serverLogic.OnTriggerDeletePoint(new WorldVector(point.transform.position.x + 0.5f, point.transform.position.y + 0.5f));
                point.SetActive(false);
            }
        }
    }


}
