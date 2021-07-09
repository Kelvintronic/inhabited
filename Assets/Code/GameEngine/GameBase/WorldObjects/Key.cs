using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Key : ServerWorldObject
    {
        public Key(WorldVector position, int colour = 0) : base(position)
        {
            switch (colour)
            {
                case 1:
                    _type = ObjectType.KeyGreen;
                    break;
                case 2:
                    _type = ObjectType.KeyBlue;
                    break;
                default:
                    _type = ObjectType.KeyRed;
                    break;
            }
        }
    }
}

