using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Gen : ServerWorldObject
    {
        public const int Levels = 3; // number of different levels the NPC has
        [Range(0, Levels - 1)]
        public byte level;

        public Gen(WorldVector position, ObjectType type) : base(position)
        {
            _canHit = true;
            _type = type;
            switch (type)
            {
                case ObjectType.Gen_level2:
                    level = 1;
                    break;
                case ObjectType.Gen_level3:
                    level = 2;
                    break;
                default:
                    level = 0;
                    _type = ObjectType.Gen_level1;
                    break;
            }
        }

        public override bool OnHit()
        {
            if (level > 0)
            {
                level--;
                _type = ObjectType.Gen_level1 + level;
                Debug.Log("Gen id '" + Id + "' devolved to level " + level);
            }
            else
                return true;

            return false;
        }

    }

}

