using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class BugNest : ServerWorldObject
    {
        public const int MaxSize = 3; // number of different levels the NPC has
        private byte _size;

        public BugNest(WorldVector position, byte size = 3) : base(position)
        {
            _canHit = true;
            _type = ObjectType.BugNest;
            _size = size;
 
        }

        public override bool OnHit()
        {
            if (_size > 0)
            {
                _size--;
                Debug.Log("BugNest id '" + Id + "' reduced in size to" + _size);
            }
            else
                return true;

            return false;
        }

        public override void Serialize(NetDataWriter writer)
        {
            _flags = _size;
            base.Serialize(writer);
        }

    }

}

