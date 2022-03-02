using GameEngine;
using GameEngine.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class AttackEventArgs : EventArgs
    {
        public byte playerId;
        public int attackerId;
        public bool ranged;
    }

    public class EyesOnEventArgs : EventArgs
    {
        public int moverId;
        public WorldVector watching;    // Absolute location of observed object
    }

    public class Monster : MonoBehaviour
    {
        [SerializeField] private Sensor _sensor;

        public event EventHandler<AttackEventArgs> Attack;
        public event EventHandler<EyesOnEventArgs> EyesOn;

        // cooldown timers
        private GameTimer _attackTimer = new GameTimer(0.75f);
        private GameTimer _intentTimer = new GameTimer(0.1f);
        private GameTimer _noPathIdleTimer = new GameTimer(1.0f);

        private WorldVector _target;
        private bool _targetValid = false;
        private Vector2 _position;

        private NPCView _NPC;

        private readonly LiteRingBuffer<WorldVector> _buffer = new LiteRingBuffer<WorldVector>(30);
        private float _timer;

        public bool IsMoving { get => _buffer.Count>1; }
        public bool HasTarget => _targetValid;
        public WorldVector Target => _target;

        public float Range => _sensor.GetRange();

        public float SensorRadius
        {
            set => _sensor.SetRadius(value);
        }

        // Start is called before the first frame update
        private void Start()
        {
            _NPC = GetComponent<NPCView>();

            _position = transform.position;
            var initialPos = new WorldVector { x = _position.x, y = _position.y };
            _buffer.Add(initialPos);
            _timer = 0;

        }

        private void Update()
        {
            CheckSensor();

            if (_buffer.Count > 1)
            {
                var posA = _buffer[0];
                var posB = _buffer[1];

                _timer += Time.deltaTime;

                var t = _timer / 0.2f;    // 0.2f = refresh rate of server object update for NPC

                if (t > 1)
                {
                    t = 1;
                    _buffer.RemoveFromStart(1);
                    _timer = 0;
                }

                // assert: t>0 && t<=1
                var newPosition = WorldVector.Lerp(posA, posB, t);

                _position.x = newPosition.x;
                _position.y = newPosition.y;
            }

            // if the monster position has been changed: apply
            if (_position != (Vector2)transform.position)
            {
                transform.position = _position;
            }
        }

        private void CheckSensor()
        {
            if (!_NPC.IsServer)
            {
                return;
            }

            // tick over cooldown timers
            _attackTimer.UpdateAsCooldown(Time.deltaTime);
            _intentTimer.UpdateAsCooldown(Time.deltaTime);

            if (!_intentTimer.IsTimeElapsed)
            {
                return;
            }
            _intentTimer.Reset();

            var closestPlayer = FindClosestPlayer();

            if (closestPlayer.id == -1)
            {
                _targetValid = false;
                return;
            }

            // sensed player is not Empty
            if (closestPlayer.heading.magnitude > 1.5f)
            {
                // we are not right next to them...

                // if we are still moving do nothing
                if (_buffer.Count > 1)
                    return;

                // if we have finished moving set server watching flag
                SetEyesOn(new WorldVector(closestPlayer.location.x, closestPlayer.location.y));
                return;
            }

            if (!_attackTimer.IsTimeElapsed)
            {
                return;
            }

            // we are right next to the player: attack
            _attackTimer.Reset();
            if (Attack != null)
            {
                Attack.Invoke(this, new AttackEventArgs
                {
                    playerId = (byte)closestPlayer.id,
                    attackerId = _NPC.Id,
                    ranged = false
                });
            }
        }

        public void Shoot()
        {
            if (Attack != null)
            {
                Attack.Invoke(this, new AttackEventArgs
                {
                    attackerId = _NPC.Id,
                    ranged = true
                });
            }
        }
        private void SetEyesOn(WorldVector target)
        {
            _target = target;
            _targetValid = true;
            if (EyesOn != null)
            {
                EyesOn.Invoke(this, new EyesOnEventArgs
                {
                    moverId = _NPC.Id,
                    watching = target
                });
            }
        }

        /// <summary>
        /// Find closest sensed human player
        /// </summary>
        /// <returns><c>SenseObject</c> with details of the closest player, <c>SenseObject.Empty</c> if none</returns>
        private SenseObject FindClosestPlayer()
        {
            if (_sensor == null)
            {
                return SenseObject.Empty;
            }

            var count = _sensor.sensedObjects.Count;

            var distance = 200f;
            var closestObject = SenseObject.Empty;

            while (count > 0)
            {
                var sense = _sensor.sensedObjects.Dequeue();
                if (sense.isHumanPlayer)
                {
                    if (sense.heading.magnitude < distance)
                    {
                        distance = sense.heading.magnitude;
                        closestObject = sense;
                    }
                }
                count--;
            }

            return closestObject;
        }

        /// <summary>
        /// Find closest sensed player and farthest sensed NPC
        /// </summary>
        /// <returns><c>(SenseObject, SenseObject)</c> tuple with closest player and farthest NPC, <c>SenseObject.Empty</c> if none</returns>
        private (SenseObject player, SenseObject NPC) FindClosestPlayerAndFarthestNPC()
        {
            var count = _sensor.sensedObjects.Count;

            var playerDistance = 100f;
            var NPCDistance = 0f;
            var closestObject = SenseObject.Empty;
            var farthestObject = SenseObject.Empty;

            while (count > 0)
            {
                var sense = _sensor.sensedObjects.Dequeue();
                if (sense.isHumanPlayer)
                {
                    if (sense.heading.magnitude < playerDistance)
                    {
                        playerDistance = sense.heading.magnitude;
                        closestObject = sense;
                    }
                }
                else
                {
                    if (sense.heading.magnitude > NPCDistance)
                    {
                        NPCDistance = sense.heading.magnitude;
                        farthestObject = sense;
                    }
                }
                count--;
            }

            return (closestObject, farthestObject);
        }

        public void UpdatePosition(WorldVector pos)
        {
            // to disable animation set the position directly and don't add to buffer

            // _position.x = pos.x;
            // _position.y = pos.y;

            _buffer.Add(pos);
        }
    }
}
