using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Door : ServerWorldObject
    {
        public Door(WorldVector position, ObjectType type, int width = 1, bool isHorizontal = true) : base(position)
        {
            _type = type;
            _width = width;
            _isHorizontal = isHorizontal;
        }

        public float GetRaycastDirection(WorldVector playerPosition, float playerRotation)
        {
            if (_isHorizontal)
            {
                if (playerPosition.y > _position.y)
                    return 180 * Mathf.Deg2Rad;
                
                return 0.0f;
            }

            if (playerPosition.x > _position.x)
                return 90 * Mathf.Deg2Rad;

            return 270 * Mathf.Deg2Rad;
        }
    }
}

