using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Conveyor : ServerWorldObject
    {
        public Conveyor(WorldVector position,int angle) : base(position)
        {
            _rotation = angle;
            _canHit = false;
            _type = ObjectType.Conveyor;
            _layer = ObjectLayer.Funcion;
            _flags = 1; // default speed
        }

        public override void Destroy()
        {

        }

    }

}

