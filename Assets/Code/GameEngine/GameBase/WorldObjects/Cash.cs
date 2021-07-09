using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Cash : ServerWorldObject
    {
        public Cash(WorldVector position) : base(position)
        {
            _type = ObjectType.Cash;
        }
    }
}

