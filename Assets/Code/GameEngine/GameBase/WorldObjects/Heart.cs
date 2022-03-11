using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Heart : ServerWorldObject
    {
        public Heart(WorldVector position) : base(position)
        {
            _canHit = true;
            _type = ObjectType.Heart;
        }

        public override bool OnHit(int playerId = -1)
        {
            return true;
        }

    }
}

