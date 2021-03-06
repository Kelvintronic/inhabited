using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Generic : ServerWorldObject
    {
        public Generic(WorldVector position, ObjectType type, int data, int width = 1, bool isHorizontal = true) : base(position)
        {
            _type = type;
            _width = width;
            _isHorizontal = isHorizontal;
            _flags = data;
        }
    }
}

