using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Health : ServerWorldObject
    {
        public Health(WorldVector position) : base(position)
        {
            _type = ObjectType.Health;
        }
    }
}

