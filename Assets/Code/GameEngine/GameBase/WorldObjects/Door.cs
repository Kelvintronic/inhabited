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
    }
}

