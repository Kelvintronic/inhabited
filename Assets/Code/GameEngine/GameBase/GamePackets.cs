using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

using UnityEngine; // for debug methods

namespace GameEngine
{
    public enum PacketType : byte
    {
        Movement,
        Activate,
        Spawn,
        ServerState,
        Serialized,
        Shoot,
        Message,
        WorldObjectState,
        NewMap
    }
    
    //Auto serializable packets
    public class JoinPacket
    {
        public string UserName { get; set; }
    }

    public class JoinAcceptPacket
    {
        public byte Id { get; set; }
        public ushort ServerTick { get; set; }
        public int Player { get; set; }
        public ushort Map { get; set; }
    }
    public class JoinRejectPacket
    {
        public byte Id { get; set; }
        public ushort ServerTick { get; set; }
        public string Reason { get; set; }
    }

    public class PlayerJoinedPacket
    {
        public string UserName { get; set; }
        public bool NewPlayer { get; set; }
        public byte Health { get; set; }
        public uint Score { get; set; }
        public int Player { get; set; }
        public ushort ServerTick { get; set; }
        public PlayerState InitialPlayerState { get; set; }
    }
    public class ActivateObjectPacket
    {
        public int objectId { get; set; }
        public ObjectType type { get; set; }
    }

    public class ReleaseObjectLockPacket
    {
        public int objectId { get; set; }
    }

    public class PickupObjectPacket
    {
        public int playerId { get; set; }
        public int objectId { get; set; }
    }

    public class TakeItemPacket
    {
        public int chestId { get; set; }
        public int slotIndex { get; set; }
    }

    public class ActivateBagItemPacket
    {
        public int slot { get; set; }
        public bool drop { get; set; }
    }

    public class RevealAreaPacket
    {
        public WorldVector position { get; set; }
        public float direction { get; set; }
    }

    public class RemoveObjectPacket
    {
        public int id { get; set; }
    }

    public class PlayerLeftPacket
    {
        public byte Id { get; set; }
    }

    public class SpawnPacket
    {
        public byte PlayerId { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    public class PlayerPositionCorrection
    {
        public float x { get; set; }
        public float y { get; set; }

    }

    //Manual serializable packets

    [Flags]
    public enum MovementKeys : byte
    {
        Left = 1 << 1,
        Right = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
        Fire = 1 << 5
    }

    public struct ShootPacket : INetSerializable
    {
        public byte FromPlayer;
        public ushort CommandId;
        public WorldVector Direction;
        public ushort ServerTick;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(FromPlayer);
            writer.Put(CommandId);
            writer.Put(Direction);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            FromPlayer = reader.GetByte();
            CommandId = reader.GetUShort();
            Direction = reader.GetWorldVector();
            ServerTick = reader.GetUShort();
        }
    }
    
    public struct PlayerInputPacket : INetSerializable
    {
        public ushort Id;
        public MovementKeys Keys;
        public WorldVector Position;
        public float Rotation;
        public bool CorrectionAccepted;
        public ushort ServerTick;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((byte)Keys);
            writer.Put(Position);
            writer.Put(Rotation);
            writer.Put(CorrectionAccepted);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetUShort();
            Keys = (MovementKeys)reader.GetByte();
            Position = reader.GetWorldVector();
            Rotation = reader.GetFloat();
            CorrectionAccepted = reader.GetBool();
            ServerTick = reader.GetUShort();
        }
    }
    
    public struct PlayerState : INetSerializable
    {
        public byte Id;
        public WorldVector Position;
        public float Rotation;
        public byte Health;
        public uint Score;
        public uint Cash;
        public ushort Tick;
        public bool Active;
        private List<PlayerBagSlot> _bag;
        public IReadOnlyList<PlayerBagSlot> Bag => _bag.AsReadOnly(); 

        public void SetBag(List<PlayerBagSlot> playerBag)
        {
            // a copy must be made in case the bag is altered in between client updates
            _bag = new List<PlayerBagSlot>(playerBag);
        }
    
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Position);
            writer.Put(Rotation);
            writer.Put(Health);
            writer.Put(Score);
            writer.Put(Cash);
            writer.Put(Tick);
            writer.Put(Active);
            if (_bag == null)
                writer.Put(0);
            else
            {
                writer.Put(_bag.Count);
                foreach(var item in _bag)
                {
                    writer.Put((int)item.type);
                    writer.Put(item.count);
                }
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Position = reader.GetWorldVector();
            Rotation = reader.GetFloat();
            Health = reader.GetByte();
            Score = reader.GetUInt();
            Cash = reader.GetUInt();
            Tick = reader.GetUShort();
            Active = reader.GetBool();

            int bagItemCount = reader.GetInt();

            if (_bag == null)
                _bag = new List<PlayerBagSlot>();
            else
                _bag.Clear(); // clear instead of creating new in case users are holding the reference

            for (int i = 0; i < bagItemCount; i++)
            {
                var item = new PlayerBagSlot();
                item.type = (PlayerBagItem)reader.GetInt();
                item.count = reader.GetInt();
                _bag.Add(item);
            }
        }
    }


    

    // Added by Kelvin 20210211
    // Updated 20210325
    public struct WorldObjectState : INetSerializable
    {
        public ushort tick;
        public int worldObjectCount;
        public List<WorldObject> worldObjects;

        public void Clear()
        {
            worldObjectCount = 0;
            if (worldObjects == null)
                worldObjects = new List<WorldObject>();
            else
                worldObjects.Clear();
        }

        public void Add(WorldObject worldObject)
        {
            worldObjectCount++;
            worldObjects.Add(worldObject);
        }

        public void Serialize(NetDataWriter writer)
        {
            if (worldObjects == null)
                Clear();
            
            writer.Put(tick);
            writer.Put(worldObjects.Count);

            foreach (var worldObject in worldObjects)
                worldObject.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Clear();

            tick = reader.GetUShort();
            worldObjectCount = reader.GetInt();
      
            for (int i = 0; i < worldObjectCount; i++)
            {
                WorldObject worldObject = new WorldObject();
                worldObject.Deserialize(reader);
                worldObjects.Add(worldObject);
            }               
        }
    }

  /*  public class NewMapPacket
    {
        public ushort Map { get; set; }

    }*/


    public struct MapPacket : INetSerializable
    {

        public ushort nMap;
        public bool isCustom;
        public MapArray mapArray;
        public int xCount;
        public int yCount;
        public WorldVector spawnPoint;
        public WorldVector exitPoint;

        public void SetCustomMap(MapArray mapArray, WorldVector spawnPoint, WorldVector exitPoint)
        {
            if (mapArray == null)
            {
                isCustom = false;
                return;
            }
            this.spawnPoint = spawnPoint;
            this.exitPoint = exitPoint;
            this.mapArray = mapArray;
            xCount = mapArray.xCount;
            yCount = mapArray.yCount;
        }

        public void SetMap(ushort map)
        {
            isCustom = false;
            nMap = map;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(isCustom);

            if (isCustom)
            {
                writer.Put(spawnPoint);
                writer.Put(exitPoint);
                writer.Put(xCount);
                writer.Put(yCount);
                for (int x = 0; x < xCount; x++)
                    for (int y = 0; y < yCount; y++)
                        writer.Put((int)mapArray.Array[x,y].type);
            }
            else
            {
                writer.Put(nMap);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            isCustom = reader.GetBool();

            if(isCustom)
            {
                spawnPoint=reader.GetWorldVector();
                exitPoint = reader.GetWorldVector();
                xCount = reader.GetInt();
                yCount = reader.GetInt();
                mapArray = new MapArray(xCount, yCount,0,0);
                for(int x=0; x<xCount;x++)
                    for (int y = 0; y <yCount; y++)
                    {
                        mapArray.Array[x, y].type = (ObjectType)reader.GetInt();
                        mapArray.Array[x, y].id = -1;
                    }
            }
            else
            {
                nMap=reader.GetUShort();
            }

        }
    }

    public struct ServerState : INetSerializable
    {
        public ushort Tick;
        public ushort LastProcessedCommand;

        public int PlayerStatesCount;
        public PlayerState[] PlayerStates;
        
        //tick
        public const int HeaderSize = sizeof(ushort)*2; 
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(LastProcessedCommand);
            writer.Put(PlayerStatesCount);
            
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[i].Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUShort();
            LastProcessedCommand = reader.GetUShort();
            PlayerStatesCount = reader.GetInt();
            
            if (PlayerStates == null || PlayerStates.Length < PlayerStatesCount)
                PlayerStates = new PlayerState[PlayerStatesCount];
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[i].Deserialize(reader);
        }
    }
}