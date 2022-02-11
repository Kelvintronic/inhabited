using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using GameEngine.Search;

namespace GameEngine
{



    public interface ILevelData
    {
        MapArray GetMapArray();
        AStarSearch GetAStarSearch();
    }

    public interface IServerData
    {
        ushort GetServerTick();
        ServerPlayerManager GetPlayerManager();
        ServerObjectManager GetObjectManager();
        MapPacket GetMapPacket();
    }

    public class ServerLogic : MonoBehaviour, IServerData, ILevelData
    {
        [SerializeField] private ClientLogic _clientLogic;
        [SerializeField] private AStarSearch _aStarSearch;

        // This is also referenced by ClientLogic
        // only needed here to get the new objectMap when maps change
        [SerializeField] private LevelSet _levelSet;


        private ServerRemoteManager _remoteManager;

        private LogicTimer _logicTimer;
        private ushort _serverTick;
        public ushort GetServerTick() => _serverTick;
        public ServerPlayerManager GetPlayerManager() => _playerManager;
        public ServerObjectManager GetObjectManager() => _objectManager;
        public MapPacket GetMapPacket() => _currentMapPacket;

        private ServerState _serverState;
        private MapPacket _currentMapPacket;

        private ServerPlayerManager _playerManager;
        private ServerObjectManager _objectManager;

        private ushort _map;
        public bool IsStarted => _remoteManager.IsRunning;

        /// <summary>
        /// Exposes <c>ServerObjectManager</c> to interested parties
        /// </summary>
        public ServerObjectManager ObjectManager
        {
            get => _objectManager;
        }

        /***************************** Unity Message Handlers **************************/
        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _logicTimer = new LogicTimer(OnLogicUpdate);
            _remoteManager = new ServerRemoteManager(this);
            _playerManager = new ServerPlayerManager(_remoteManager);
            _objectManager = new ServerObjectManager(_remoteManager, _playerManager);

            _map = 0;
        }

        public void OnDestroy()
        {
            _remoteManager.Stop();
            _logicTimer.Stop();
        }

        public void Update()
        {
            if (IsStarted)
            {
                // toggle debug layer in current level
                var currentLevel = _levelSet.GetCurrentLevel();
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                    currentLevel.ShowDebugMap(!currentLevel.IsDebugVisible);
                if (currentLevel.IsDebugVisible)
                    currentLevel.SetDebugMap(_objectManager.MapArray);
            }
            _remoteManager.PollEvents();
            _logicTimer.Update();
        }

        /**************************  ILevelData methods *************************************/

        public MapArray GetMapArray()
        {
            return _levelSet.GetCurrentLevel().GetMapArray();
        }

        public AStarSearch GetAStarSearch()
        {
            return _aStarSearch;
        }

        /**************************  UI Control methods *************************************/
        public void StartServer()
        {
            if (!IsStarted)
            {
                _map = 0;
                SetMap();
                _remoteManager.Start();
                _logicTimer.Start();
            }
        }

        public void StopServer()
        {
            _remoteManager.DisconnectAll();
            _remoteManager.Stop();
            _logicTimer.Stop();
            _playerManager.RemoveAllPlayers();
            _aStarSearch.Stop();
            _map = 0;
        }

        public void JumpToLevel(int levelIndex)
        {
            JumpToMap((ushort)levelIndex);
        }

        /**************************  Main server loop  **************************************/

        // experimental variables
        int turn = 0;

        private void OnLogicUpdate()
        {
            // increment tick and wrap to zero at MaxGameSequence (1024)
            _serverTick = (ushort)((_serverTick + 1) % NetworkGeneral.MaxGameSequence);

            // Update players
            _playerManager.LogicUpdate();

            // add chest for any players who have died this tick
            foreach (ServerPlayer player in _playerManager)
            {
                if (player.deadThisTick && player.Bag.Count > 0)
                {
                    Chest chest = (Chest)_objectManager.CreateWorldObject(ObjectType.Chest, player.Position);
                    foreach (var item in player.Bag)
                        chest.AddItem(item.type, item.count);
                    _objectManager.AddWorldObject(chest);
                }
                else

                // experiment to test position correction logic
                if(player.player==0&&player.IsAlive&&player.IsActive&&_serverTick%4==0)
                {
              /*      float deltax=0, deltay=0;
                    switch(turn)
                    {
                        case 0:
                            deltax++;
                            break;
                        case 1:
                            deltay++;
                            break;
                        case 2:
                            deltax--;
                            break;
                        case 3:
                            deltay--;
                            break;
                    }
                    turn++;
                    if (turn > 3)
                        turn = 0;

                    _remoteManager.SendToPeer(player.AssociatedPeer, new PlayerPositionCorrection {x=deltax, y=deltay});
                    */
                }
            }

            // Update objects (movement and such)
            _objectManager.LogicUpdate();


            // on only even tick values
            // Send to each client a server state
            if (_serverTick % 2 == 0)
            {
                // if all players are inactive then we need to move to the next map
                if (_playerManager.Count > 0 && _playerManager.GetInactivePlayers() == _playerManager.Count)
                {
                    _map++;
                    if (_map > 5)
                        _map = 0; // wrap arround

                    JumpToMap(_map);

                /*    var level = _levelSet.GetCurrentLevel();
                    var chestLocation = level.GetSpawnPoint() + new Vector2(5f, 5f);

                    // throw a chest into the map for testing
                    Chest chest = (Chest)_objectManager.CreateWorldObject(ObjectType.Chest, new WorldVector(chestLocation.x, chestLocation.y));
                    chest.AddItem(PlayerBagItem.Bomb, 1);
                    chest.AddItem(PlayerBagItem.Health, 1);
                    chest.AddItem(PlayerBagItem.KeyRed, 1);
                    _objectManager.AddWorldObject(chest);*/
                }

                // send object updates if any
                _objectManager.ProcessUpdateQueue(_serverTick);

                _serverState.Tick = _serverTick;
                _serverState.PlayerStates = new PlayerState[_playerManager.Count];
                _serverState.PlayerStatesCount = _playerManager.Count;

                // transfer playerstate slots to a sequential array of player states
                int s = 0;
                foreach (ServerPlayer p in _playerManager)
                {
                    _serverState.PlayerStates[s] = _playerManager.PlayerStates[p.player];
                    s++;
                }

                // send state to each player
                foreach (ServerPlayer p in _playerManager)
                {
                    _serverState.LastProcessedCommand = p.LastProcessedCommandId;
                    p.AssociatedPeer.Send(_remoteManager.WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Unreliable);
                }
            }

        }

        private void JumpToMap(ushort nMap)
        {

            _map = nMap;

            SetMap();

            // Send new map packet to all except our client
            foreach (ServerPlayer p in _playerManager)
            {
                if (p.Id!=0)
                    _remoteManager.SendToPeer(p.AssociatedPeer, PacketType.NewMap, _currentMapPacket);

                // and reset each player for new level
                p.NewLevelReset();
                _playerManager.PlayerStates[p.player].Active = true;
            }

            // Tell our client to set the new map
            _clientLogic.OnNewMap(_currentMapPacket);

        }
        private void SetMap()
        {
            _levelSet.SetMap(_map);

            // We can now be sure that the new map is active in our client so...
            Level currentLevel = _levelSet.GetCurrentLevel();

            _currentMapPacket = new MapPacket { isCustom = _levelSet.IsCustomLevel, nMap = _map };
            if(_levelSet.IsCustomLevel)
            {
                _currentMapPacket.SetCustomMap(currentLevel.GetMapArray(),
                                            new WorldVector(currentLevel.GetSpawnPoint().x, currentLevel.GetSpawnPoint().y),
                                            new WorldVector(currentLevel.GetExitPoint().x, currentLevel.GetExitPoint().y));
            }

            // Reinitialise the object manager for the new level
            _objectManager.Reset(this);

            // Get the new ObjectMap and create the new map objects
            MapArray objectMapArray = currentLevel.GetObjectArray();
            for (int y = 0; y < objectMapArray.yCount; y++)
                for (int x = 0; x < objectMapArray.xCount; x++)
                {
                    if (objectMapArray.Array[x, y].type == ObjectType.None)
                        continue;
                    // if the co-ordinate has been excepted igore it
                    if (objectMapArray.IsException(x, y))
                        continue;

                    // if object is a door or false wall check for line of same object
                    bool isHorizontal = true;
                    int width = 1;
                    if (objectMapArray.Array[x, y].type == ObjectType.DoorBlue ||
                        objectMapArray.Array[x, y].type == ObjectType.DoorGreen ||
                        objectMapArray.Array[x, y].type == ObjectType.DoorRed ||
                        objectMapArray.Array[x, y].type == ObjectType.Door ||
                        objectMapArray.Array[x, y].type == ObjectType.HiddenDoor ||
                        objectMapArray.Array[x, y].type == ObjectType.FalseWall)
                    {
                        if(objectMapArray.Array[x, y].type==ObjectType.FalseWall)
                        {
                            Debug.Log("False wall found");
                        }
                        width = objectMapArray.CountCommonCells(x, y);
                        if (width == 1)
                        {
                            width = objectMapArray.CountCommonCells(x, y, false);
                            if (width > 1)
                                isHorizontal = false;
                        }
                    }
                    // create object
                    var worldObject = _objectManager.CreateWorldObject(objectMapArray.Array[x, y].type, 
                                                    new WorldVector(x + objectMapArray.xOffset + 0.5f, y + objectMapArray.yOffset + 0.5f),
                                                    width, isHorizontal, objectMapArray.Array[x, y].data);

                    if (worldObject != null)
                    {
                        if(!_objectManager.AddWorldObject(worldObject))
                        {
                            Debug.Log($"[S] Failed to add object of type '{worldObject.Type}' at:'{worldObject.Position.x}','{worldObject.Position.y}");
                        }
                    }

                }
        }

        // reacts to a ServerBolt hit
        public void OnBoltHit(object sender, ServerBoltArg e)
        {
            var worldObject=_objectManager.GetById(e.objectId);

            if (worldObject == null)
                return;

            if (!worldObject.CanHit)
                return;

            // Get score factor
            uint scoreFactor = 0;
            switch(worldObject.Type)
            {
                case ObjectType.NPCBug:
                    scoreFactor = 1;
                    break;
                case ObjectType.NPCMercenary:
                case ObjectType.NPCTrader:
                    scoreFactor = 2;
                    break;
                case ObjectType.BugNest:
                    scoreFactor = 5;
                    break;
            }

            if (worldObject.OnHit())
            {
                _objectManager.RemoveObject(worldObject.Id);

                switch(worldObject.Type)
                {
                    case ObjectType.Bomb:
                        Bomb.Explode(_objectManager, worldObject.Position);
                        break;
                    case ObjectType.Heart:
                        if(!_playerManager.ResurrectNextDeadPlayer(worldObject.Position))
                        {
                            var newNPC = (NPCBug)_objectManager.CreateWorldObject(ObjectType.NPCBug, worldObject.Position);
                            _objectManager.AddWorldObject(newNPC);
                        }
                        break;
                }
            }
            else
                _objectManager.SetUpdate(worldObject.Id);

            var shooter = _playerManager.GetById(e.playerId);
            if (shooter != null)
            {
                shooter.AddScore(scoreFactor * 10 + 10);

                // TODO: Implement score update for clients?
            }
        }

        // reacts to a Monster Attack
        public void OnMonsterAttack(object sender, AttackEventArgs e)
        {
            var worldObject = _objectManager.GetById(e.attackerId);
            if (worldObject == null)
                return;

            // Get score factor
            uint damageFactor = 0;
            switch (worldObject.Type)
            {
                case ObjectType.NPCMercenary:
                    damageFactor = 3;
                    break;
                case ObjectType.NPCBug:
                    damageFactor = 1;
                    break;
                case ObjectType.NPCTrader:
                    damageFactor = 5;
                    break;
            }

            var player = _playerManager.GetById(e.playerId);
            if(player!=null)
                player.SubtractHealth((byte)(damageFactor * 2 + 2));
        }
        
        // reacts to a Monster eye movement
        public void OnMonsterWatching(object sender, EyesOnEventArgs e)
        {
            NPC npcObject = (NPC)_objectManager.GetById(e.moverId);

            if (npcObject != null)
            {
                npcObject.SetWatching(e.watching);
            }
        }

        // reacts to a Generator Spawn
        public void OnSpawnBug(object sender, SpawnBugEventArgs e)
        {
            BugNest genObject = (BugNest)_objectManager.GetById(e.generatorId);
            if (genObject == null)
                return;

            var newNPC = (NPC)_objectManager.CreateWorldObject(ObjectType.NPCBug, e.position);

            _objectManager.AddWorldObject(newNPC);
        }

        // reacts to a Trigger if it is set to remove an object
        public void OnTriggerDeletePoint(WorldVector position)
        {
            int id = _objectManager.FindObjectByPosition(position);
            if(id!=-1)
                _objectManager.RemoveObject(id);
            else
                Debug.Log("[S] Object not found at position: x=" + position.x + " y=" +position.y);

        }
    }
}