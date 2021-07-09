using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Bomb : ServerWorldObject
    {
        public Bomb(WorldVector position) : base(position)
        {
            _canHit = true;
            _type = ObjectType.Bomb;
        }

        public override bool OnHit()
        {
            return true;
        }

        public static void Explode(ServerObjectManager objectManager,WorldVector position)
        {
            var destroyedItems = new List<int>();
            foreach (var worldObject in objectManager)
            {
                switch(worldObject.Type)
                {
                    case ObjectType.NPC_level1:
                    case ObjectType.NPC_level2:
                    case ObjectType.NPC_level3:
                        if ((worldObject.Position - position).Length() < 10.0f)
                        destroyedItems.Add(worldObject.Id);
                        break;
                }
            }
            foreach (int id in destroyedItems)
                objectManager.RemoveObject(id);
        }

        public override void Destroy()
        {
            var celPos=_mapArray.GetCellVector(_position);
            _mapArray.SetCell(celPos, MapCell.Empty);
        }

    }

}

