using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class BugNest : ServerWorldObject
    {
        private bool _isOpen;
        private byte _health;

        public BugNest(WorldVector position) : base(position)
        {
            _canHit = true;
            _type = ObjectType.BugNest;
            _isOpen = true;
            _health = 3;
 
        }

        public override bool OnHit()
        {
            if (_health > 0)
            {
                _health--;
                Debug.Log("BugNest id '" + Id + "' reduced in size to" + _health);
            }

            return false;
        }

        public override bool Update(float delta)
        {
            // if we have been hit and health is depleted notify client
            if(_health==0&&_isOpen)
            {
                _isOpen = false;
                _update = true;
            }
                
            return base.Update(delta);
        }


        public override void Serialize(NetDataWriter writer)
        {
            _flags = _health;
            base.Serialize(writer);
        }

    }

}

