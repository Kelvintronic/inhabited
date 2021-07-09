using System;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GameEngine
{
    public interface INotificationManager
    {
        void AddDestroyNotification(int requesterId, int objectId);
    }

    public class ServerObjectManager : WorldObjectManagerBase, INotificationManager
    {
        private readonly Dictionary<int, WorldObject> _worldObjects;
        private Queue<int> _updates;
        
        public override int Count => _worldObjects.Count;
        public Queue<int> PendingUpdates => _updates;

        // Transmission interface and packet
        private INetServer _netSender;
        private WorldObjectState _worldObjectState;

        // world info needed by objects for AI
        private ILevelData _levelData;
        private MapArray _mapArray
        {
            get
            {
                return _levelData.GetMapArray();
            }
        }

        private IEnumerable<BasePlayer> _playerList;

        public MapArray MapArray => _mapArray;

        // interactables dictionary
        // items that are classed interactable are referenced in this instead of _mapArray
        private Dictionary<VectorInt, int> _interactables;

        public ServerObjectManager(INetServer netSender, IEnumerable<BasePlayer> playerList)
        {
            _netSender = netSender;
            _playerList = playerList;

            _worldObjects = new Dictionary<int, WorldObject>();
            _interactables = new Dictionary<VectorInt, int>();
            _updates = new Queue<int>();
        }

        public override IEnumerator<WorldObject> GetEnumerator()
        {
            foreach (var ph in _worldObjects)
                yield return ph.Value;
        }

        public WorldObject GetById(int id)
        {
            return _worldObjects.TryGetValue(id, out var wo) ? wo : null;
        }

        public WorldObject RemoveObject(int id)
        {
            if (_worldObjects.TryGetValue(id, out var wo))
            {
                ((ServerWorldObject)wo).Destroy();                          // cleanup map array
                foreach(var notifyId in ((ServerWorldObject)wo).DestroyNotifications)
                {
                    Debug.Log("Destroy with notification");
                    if (_worldObjects.TryGetValue(notifyId, out var nwo))
                        ((ServerWorldObject)nwo).DestroyNotification();

                }
                if (wo.IsInteractable)
                    _interactables.Remove(wo.PositionInt);                  // remove interactable reference
                _worldObjects.Remove(id);                                   // remove object
                _netSender.SendToAll(new RemoveObjectPacket { id = id });   // inform clients
            }
        
            return wo;
        }

        public int FindObjectByPosition(WorldVector position)
        {
            MapCell cell;
            try
            {
                cell = _mapArray.GetCell(position);
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1; 
            }

            return cell.id;
        }

        public void Reset(ILevelData levelData)
        {
            _worldObjects.Clear();
            _interactables.Clear();
            _updates.Clear();
            _levelData = levelData;

            // initialise search algorithm if level has it attached
            var aStar = levelData.GetAStarSearch();
            if (aStar != null)
                aStar.Initialise(levelData.GetMapArray());

        }

        // called to update all objects with their fixed update
        public override void LogicUpdate()
        {
            foreach (var wo in _worldObjects)
            {
                if (wo.Value.Update(LogicTimer.FixedDelta))
                    _updates.Enqueue(wo.Value.Id);
            }
        }

        public bool AddWorldObject(WorldObject worldObject)
        {

            if (worldObject.IsInteractable) // add interactables reference
            {
                // Don't add if there is an object already at the location
                if (_interactables.ContainsKey(worldObject.PositionInt))
                    return false;

                worldObject.SnapToGrid();

                _interactables.Add(worldObject.PositionInt, worldObject.Id);
            }
            else // add map array reference(s)
            {
                // get potential location of the object in the array
                worldObject.SnapToGrid();
                var celPos = _mapArray.GetCellVector(worldObject.Position);

                // Don't add if there is an object already at the location
                if (_mapArray.Array[celPos.x, celPos.y].type != ObjectType.None)
                    return false;

                // deal with the width of the object
                for (int i = 0; i < worldObject.Width; i++)
                {
                    _mapArray.SetCell(celPos, new MapCell { id = worldObject.Id, type = worldObject.Type });
                    if (worldObject.IsHorizontal)
                        celPos.x++;
                    else
                        celPos.y++;
                }
            }

            _worldObjects.Add(worldObject.Id, worldObject);
            _updates.Enqueue(worldObject.Id);
            return true;
        }

        // called when a worldObject has changed externally
        public void SetUpdate(int objectId)
        {
            _updates.Enqueue(objectId);
        }

        // called by server logic during it's update cycle
        public void ProcessUpdateQueue(ushort serverTick)
        {
            if (_updates.Count == 0)
                return;

            _worldObjectState.Clear();

            _worldObjectState.tick = serverTick;

            while (_updates.Count > 0)
            {
                var nextId = _updates.Dequeue();
                var worldObject = GetById(nextId);
                if (worldObject != null)
                    _worldObjectState.Add(worldObject);
            }

            _netSender.SendToAll(PacketType.WorldObjectState, _worldObjectState);
        }

        /// <summary>
        /// Send all objects to a single client
        /// </summary>
        /// <param name="peer"></param>
        public void UpdateClient(NetPeer peer)
        {
            _worldObjectState.Clear();

            _worldObjectState.tick = 0;

            foreach(var wo in _worldObjects)
                _worldObjectState.Add(wo.Value);

            _netSender.SendToPeer(peer,PacketType.WorldObjectState, _worldObjectState);
        }

        public WorldObject CreateWorldObject(ObjectType type, WorldVector position, int width = 1, bool isHorizontal = true)
        {
            ServerWorldObject newObject = null;
            switch (type)
            {
                case ObjectType.KeyRed:
                    newObject = new Key(position);
                    break;
                case ObjectType.KeyGreen:
                    newObject = new Key(position, 1);
                    break;
                case ObjectType.KeyBlue:
                    newObject = new Key(position, 2);
                    break;
                case ObjectType.Bomb:
                    newObject = new Bomb(position);
                    break;
                case ObjectType.Heart:
                    newObject = new Heart(position);
                    break;
                case ObjectType.DoorRed:
                case ObjectType.DoorGreen:
                case ObjectType.DoorBlue:
                    newObject = new Door(position, type - ObjectType.DoorRed, width, isHorizontal);
                    break;
                case ObjectType.Health:
                    newObject = new Health(position);
                    break;
                case ObjectType.Cash:
                    newObject = new Cash(position);
                    break;
                case ObjectType.FalseWall:
                    newObject = new Generic(position, type, width, isHorizontal);
                    break;

                // NPCs and Generators
                case ObjectType.Gen_level1:
                case ObjectType.Gen_level2:
                case ObjectType.Gen_level3:
                    newObject = new Gen(position, type);
                    break;
                case ObjectType.NPC_level1:
                case ObjectType.NPC_level2:
                case ObjectType.NPC_level3:
                    newObject = new NPC(position, type, this);
                    break;
                case ObjectType.Chest:
                    newObject = new Chest(position);
                    break;
            }
            if(newObject!=null)
            {
                newObject.SetGameReferences(_levelData, _playerList);
                return newObject;
            }
            return null;
        }

        public void AddDestroyNotification(int requesterId, int objectId)
        {
            var obj = (ServerWorldObject)GetById(objectId);
            if(obj!=null)
                obj.AddDestroyNotification(requesterId);
        }
    }
}