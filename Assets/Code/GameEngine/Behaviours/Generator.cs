using GameEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class SpawnMonsterEventArgs : EventArgs
    {
        public int generatorId;
        public WorldVector position;
    }

    public class Generator : MonoBehaviour
    {
        private float _elapsedTime = 0;
        private GenView _view;
        private Collider2D[] _overlapResult = new Collider2D[1];

        [Range(0f, 1f)]
        [Tooltip("Probability of generating an NPC")]
        public float probability = 0.5f;
        [Tooltip("Seconds between each generation attempt")]
        public float period = 5f;

        public event EventHandler<SpawnMonsterEventArgs> Spawn;

        private int _spawnCount = 0;

        // Start is called before the first frame update
        void Start()
        {
            _view = GetComponent<GenView>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_view.IsServer)
            {
                _elapsedTime += Time.deltaTime;

                if (_elapsedTime >= period)
                {
                    if (UnityEngine.Random.Range(0f, 1f) <= probability)
                    {
                        // try 5 times to find a suitable spawn point
                        for (int i = 0; i < 5; i++)
                        {
                            var candidate = (Vector2)gameObject.transform.position +
                                (((UnityEngine.Random.insideUnitCircle).normalized) * UnityEngine.Random.Range(1f, 2f));
                            var overlapCircleResult = Physics2D.OverlapCircleNonAlloc(candidate, 0.25f,
                                _overlapResult, 1 << LayerMask.NameToLayer("NPC")); // only check NPC layer

                            if (overlapCircleResult == 0)
                            {
                                //Debug.Log($"[S] Generator id '{_view.Id}' spat out a level '{_view.level}' monster. BOO!");

                                if (Spawn != null)
                                {
                                    Spawn.Invoke(this, new SpawnMonsterEventArgs
                                    {
                                        generatorId = _view.Id,
                                        position = new WorldVector(candidate.x, candidate.y)
                                    });
                                    _spawnCount++;
                                    // every 10 spawns slow the spawn rate
                                    if (_spawnCount % 10 == 0)
                                        period += 5.0f;
                                }

                                break;
                            }
                        }
                    }

                    _elapsedTime = 0;
                }
            }
        }
    }
}