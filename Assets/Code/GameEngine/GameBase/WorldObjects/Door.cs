using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Door : ServerWorldObject
    {
        public Door(WorldVector position,int colour = 0, int width = 1, bool isHorizontal = true) : base(position)
        {
            switch (colour)
            {
                case 1:
                    _type = ObjectType.DoorGreen;
                    break;
                case 2:
                    _type = ObjectType.DoorBlue;
                    break;
                default:
                    _type = ObjectType.DoorRed;
                    break;
            }
            _width = width;
            _isHorizontal = isHorizontal;
        }
    }
}

