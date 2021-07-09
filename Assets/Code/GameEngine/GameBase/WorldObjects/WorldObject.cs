﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LiteNetLib.Utils;

namespace GameEngine
{

    public class WorldObject : INetSerializable
    {
        // variables contributing to _size
        private int _id;
        protected ObjectType _type;
        protected WorldVector _position;
        protected float _rotation;
        protected bool _active;
        protected bool _canHit = false;
        protected bool _isInteractable = false;   // true if to be placed on the interactable layer only

        // currently only used for doors
        protected int _width = 1;
        protected bool _isHorizontal = true;

        // Variables not contributing to Size
        public WorldVector Position => _position;
        public VectorInt PositionInt { get => new VectorInt
        {
            x = (int)Math.Floor(_position.x),
            y = (int)Math.Floor(_position.y)
        }; }
        public float Rotation => _rotation;
        public bool IsActive => _active;
        public ObjectType Type => _type;
        public bool CanHit => _canHit;
        public bool IsInteractable => _isInteractable;
        public int Width => _width;
        public bool IsHorizontal => _isHorizontal;

        public int Id { get => _id; }

        private static int _nextid = 42;

        // unique data required by clients
        protected INetSerializable _data = null;

        protected WorldObject(WorldVector position)
        {
            _id = _nextid++;
            _position = position;
            _active = true;
        }

        // required to deserialise
        public WorldObject()
        {

        }

        public object GetDataObject()
        {
            return _data;
        }

        public void SnapToGrid()
        {
            _position.x = PositionInt.x+0.5f;
            _position.y = PositionInt.y+0.5f;
        }

        public virtual void SetActive(bool active)
        {
            _active = active;
        }

        public virtual void SetPosition(WorldVector newPosition)
        {
            _position = newPosition;
        }

        // should return true if object is able to be destroyed
        public virtual bool OnHit()
        {
            return false;
        }

        // returns true if object has changed
        public virtual bool Update(float delta)
        {
            return false;
        }

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(_id);
            writer.Put((int)_type);
            writer.Put(_position);
            writer.Put(_rotation);
            writer.Put(_active);
            writer.Put(_canHit);
            writer.Put(_isInteractable);
            writer.Put(_width);
            writer.Put(_isHorizontal);

            if (_data != null)
                _data.Serialize(writer);
        }
        public virtual void Deserialize(NetDataReader reader)
        {
            _id = reader.GetInt();
            _type = (ObjectType)reader.GetInt();
            _position = reader.GetWorldVector();
            _rotation = reader.GetFloat();
            _active = reader.GetBool();
            _canHit = reader.GetBool();
            _isInteractable = reader.GetBool();
            _width = reader.GetInt();
            _isHorizontal = reader.GetBool();

            switch(_type)
            {
                case ObjectType.Chest:
                    if (_data == null)
                        _data = new ChestData();
                    break;
            }

            if (_data != null)
                _data.Deserialize(reader);
        }

    }

}

