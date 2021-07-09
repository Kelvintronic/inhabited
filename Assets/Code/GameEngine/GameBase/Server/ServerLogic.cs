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

    public interface INetServer
    {
        void SendToAll<T>(PacketType type, T packet) where T : struct, INetSerializable;
        void SendToAll<T>(T packet) where T : class, new();
        void SendToPeer<T>(NetPeer peer, PacketType type, T packet) where T : struct, INetSerializable;
        void SendToPeer<T>(NetPeer peer, T packet) where T : class, new();
        NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable;
        NetDataWriter WritePacket<T>(T packet) where T : class, new();

    }

    public interface ILevelData
    {
        MapArray GetMapArray();
        AStarSearch GetAStarSearch();
    }

    public class ServerLogic : MonoBehaviour, INetEventListener, INetServer, ILevelData
    {
        [SerializeField] private ClientLogic _clientLogic;
        [SerializeField] private AStarSearch _aStarSearch;

        // This is also referenced by ClientLogic
        // only needed here to get the new objectMap when maps change
        [SerializeField] private LevelSet _levelSet;

        private NetManager _netManager;
        private NetPacketProcessor _packetProcessor;

        private LogicTimer _logicTimer;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();
        private ushort _serverTick;

        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        private ActivateObjectPacket _cachedActivateCommand = new ActivateObjectPacket();
        private ServerState _serverState;
        private NewMapPacket _currentMapData;

        private ServerPlayerManager _playerManager;
        private ServerObjectManager _objectManager;

        private ushort _map;
        public ushort Tick => _serverTick;
        public bool IsStarted => _netManager.IsRunning;

        public bool IsDestroyKeyOnUse;

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
            _packetProcessor = new NetPacketProcessor();
            _playerManager = new ServerPlayerManager(this);
            _objectManager = new ServerObjectManager(this, _playerManager);

            //register auto serializable vector2
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetWorldVector());

            //register auto serializable PlayerState 
            _packetProcessor.RegisterNestedType<PlayerState>(); // this allows PlayerState to be nested in the PlayerJoinedPacket
            _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
            _packetProcessor.SubscribeReusable<ActivateObjectPacket, NetPeer>(OnActivateObjectReceived);
            _packetProcessor.SubscribeReusable<ReleaseObjectLockPacket, NetPeer>(OnReleaseObjectLockReceived);
            _packetProcessor.SubscribeReusable<PickupObjectPacket, NetPeer>(OnPickupObjectReceived);
            _packetProcessor.SubscribeReusable<ActivateBagItemPacket, NetPeer>(OnActivateBagItemPacket);
            _packetProcessor.SubscribeReusable<TakeItemPacket, NetPeer>(OnTakeItemPacket);

            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };

            _map = 0;
        }

        public void OnDestroy()
        {
            _netManager.Stop();
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
            _netManager.PollEvents();
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
                _netManager.Start(10515);
                _logicTimer.Start();
            }
        }

        public void StopServer()
        {
            _netManager.DisconnectAll();
            _netManager.Stop();
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

                    var level = _levelSet.GetCurrentLevel();
                    var chestLocation = level.GetSpawnPoint() + new Vector2(5f, 5f);

                    // throw a chest into the map for testing
                    Chest chest = (Chest)_objectManager.CreateWorldObject(ObjectType.Chest, new WorldVector(chestLocation.x, chestLocation.y));
                    chest.AddItem(PlayerBagItem.Bomb, 1);
                    chest.AddItem(PlayerBagItem.Health, 1);
                    chest.AddItem(PlayerBagItem.KeyRed, 1);
                    _objectManager.AddWorldObject(chest);
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
                    p.AssociatedPeer.Send(WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Unreliable);
                }
            }

        }



        /********************* Packet sending helpers ****************************/
        public NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte)type);
            packet.Serialize(_cachedWriter);
            return _cachedWriter;
        }

        public NetDataWriter WritePacket<T>(T packet) where T : class, new()
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte)PacketType.Serialized);
            _packetProcessor.Write(_cachedWriter, packet);
            return _cachedWriter;
        }

        public void SendToAll<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            foreach (ServerPlayer p in _playerManager)
                p.AssociatedPeer.Send(WriteSerializable(type, packet), DeliveryMethod.ReliableOrdered);
        }

        public void SendToAll<T>(T packet) where T : class, new()
        {
            _netManager.SendToAll(WritePacket(packet), DeliveryMethod.ReliableOrdered);
        }

        public void SendToPeer<T>(NetPeer peer, PacketType type, T packet) where T : struct, INetSerializable
        {
            peer.Send(WriteSerializable(type, packet), DeliveryMethod.ReliableOrdered);
        }

        public void SendToPeer<T>(NetPeer peer, T packet) where T : class, new()
        {
            peer.Send(WritePacket(packet), DeliveryMethod.ReliableOrdered);
        }

        /********************** Client methods **********************************/
        private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
        {
            //   Debug.Log("[S] Join packet received: " + joinPacket.UserName);
            var player = new ServerPlayer(joinPacket.UserName, peer);
            if (!_playerManager.AddPlayer(player))
            {
                var jr = new JoinRejectPacket { Id = player.Id, ServerTick = _serverTick };
                peer.Send(WritePacket(jr), DeliveryMethod.ReliableOrdered);
                return;
            }

            player.Spawn(MathFloat.Random(-2f, 2f), MathFloat.Random(-2f, 2f));

            //Send join accept
            var ja = new JoinAcceptPacket { Id = player.Id, ServerTick = _serverTick, Player = player.player, Map = _map };
            peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);

            //Send to old players info about new player
            var pj = new PlayerJoinedPacket
            {
                UserName = joinPacket.UserName,
                NewPlayer = true,
                InitialPlayerState = player.NetworkState,
                ServerTick = _serverTick,
                Player = player.player,
                Health = player.Health,
                Score = player.Score

            };
            _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);

            //Send to new player info about old players
            pj.NewPlayer = false;
            foreach (ServerPlayer otherPlayer in _playerManager)
            {
                if (otherPlayer == player)
                    continue;
                pj.UserName = otherPlayer.Name;
                pj.InitialPlayerState = otherPlayer.NetworkState;
                pj.Player = otherPlayer.player;
                pj.Health = otherPlayer.Health;
                pj.Score = otherPlayer.Score;
                peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
            }

            // Send new player current level data (only if player is not hosting)
            if (player.Id!=0)
                SendToPeer(peer, PacketType.NewMap, _currentMapData);

            // Send to new player current level objects
            _objectManager.UpdateClient(peer);
        }

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            _cachedCommand.Deserialize(reader);
            var player = (ServerPlayer)peer.Tag;

            player.ApplyInput(_cachedCommand, LogicTimer.FixedDelta);

            if ((_cachedCommand.Keys & MovementKeys.Fire) != 0)
            {
                if (player.ApplyShoot())
                {
                    WorldVector dir = new WorldVector(MathFloat.Cos(player.Rotation), MathFloat.Sin(player.Rotation));

                    ShootPacket sp = new ShootPacket
                    {
                        FromPlayer = player.Id,
                        CommandId = player.LastProcessedCommandId,
                        ServerTick = Tick,
                        Direction = dir
                    };

                    _netManager.SendToAll(WriteSerializable(PacketType.Shoot, sp), DeliveryMethod.ReliableUnordered);
                }
            }

        }

        private void OnActivateObjectReceived(ActivateObjectPacket activatePacket, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            var player = (ServerPlayer)peer.Tag;

            // find object to be activated
            ServerWorldObject worldObject = (ServerWorldObject)_objectManager.GetById(activatePacket.objectId);

            if (worldObject == null)
            {
                // exceptions here
                switch (activatePacket.type)
                {
                    case ObjectType.ExitPoint:
                        player.ApplyActivate(activatePacket);
                        break;
                }
                return;
            }

            if (!worldObject.IsActive)
                return;

            // if object is found
            if (player.ApplyActivate(activatePacket))
            {
                // if activation is successfull do server action
                switch (worldObject.Type)
                {
                    case ObjectType.DoorBlue:
                        _objectManager.RemoveObject(activatePacket.objectId);
                        if (IsDestroyKeyOnUse)
                            player.RemoveBagItem(PlayerBagItem.KeyBlue);
                        break;
                    case ObjectType.DoorRed:
                        _objectManager.RemoveObject(activatePacket.objectId);
                        if (IsDestroyKeyOnUse)
                            player.RemoveBagItem(PlayerBagItem.KeyRed);
                        break;
                    case ObjectType.DoorGreen:
                        _objectManager.RemoveObject(activatePacket.objectId);
                        if(IsDestroyKeyOnUse)
                            player.RemoveBagItem(PlayerBagItem.KeyGreen);
                        break;
                    case ObjectType.Chest:
                        if (worldObject.Lock(player))
                        {
                            // send packet back to player if lock is successfull
                            peer.Send(WritePacket(activatePacket), DeliveryMethod.ReliableOrdered);
                            return;
                        }
                        break;
                }
            }
        }

        private void OnReleaseObjectLockReceived(ReleaseObjectLockPacket unlockPacket, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            var player = (ServerPlayer)peer.Tag;

            // find object to be activated
            ServerWorldObject worldObject = (ServerWorldObject)_objectManager.GetById(unlockPacket.objectId);

            worldObject.Unlock(player);

        }

        private void OnPickupObjectReceived(PickupObjectPacket pickupPacket, NetPeer peer)
        {
            //   Debug.Log("[S] Pickup packet received: " + joinPacket.UserName);
            if (peer.Tag == null)
                return;
            var player = (ServerPlayer)peer.Tag;

            // find object to be picked up
            var worldObject = _objectManager.GetById(pickupPacket.objectId);

            if (worldObject == null)
                return;

            if (!worldObject.IsActive)
                return;

            // if object is found
            if (player.ApplyPickup(worldObject))
                _objectManager.RemoveObject(worldObject.Id);
        }

        private void OnActivateBagItemPacket(ActivateBagItemPacket packet, NetPeer peer)
        {
            //   Debug.Log("[S] Pickup packet received: " + joinPacket.UserName);
            if (peer.Tag == null)
                return;
            var player = (ServerPlayer)peer.Tag;

            var item = player.ApplyUseBagItem(packet.slot, packet.drop);

            if (item == PlayerBagItem.Lint)
                return;

            if (packet.drop)
            {
                // TODO: Check if object exists at location and if so alter new object position

                var lookDirection = player.GetLookVector();
                lookDirection.Normalize();
                lookDirection = lookDirection * 1.5f;
                lookDirection += player.Position;

                // create object
                var worldObject = _objectManager.CreateWorldObject((ObjectType)item, lookDirection);
                if (worldObject != null)
                {
                    // Try to add object to world and if fail
                    // put it back in the player bag
                    if (!_objectManager.AddWorldObject(worldObject))
                        player.AddBagItem(item);
                }

                return;
            }

            // Do server action if item affects the world
            switch (item)
            {
                case PlayerBagItem.Bomb:
                    Bomb.Explode(_objectManager, player.Position);
                    break;
            }
        }

        private void OnTakeItemPacket(TakeItemPacket packet, NetPeer peer)
        {
            //   Debug.Log("[S] Pickup item received: ");
            if (peer.Tag == null)
                return;
            var player = (ServerPlayer)peer.Tag;

            // find object to be activated
            Chest chestObject = (Chest)_objectManager.GetById(packet.chestId);

            if (chestObject == null)
                return;

            // confirm object is a chest
            if (chestObject.Type != ObjectType.Chest)
                return;

            // check the player has room
            if (player.IsBagFull)
                return;

            // remove item and add to player bag
            player.AddBagItem(chestObject.RemoveItem(packet.slotIndex));

            // if lock has been removed, notify client object to release player
            if (!chestObject.IsLocked)
                peer.Send(WritePacket(new ReleaseObjectLockPacket { objectId = chestObject.Id }), DeliveryMethod.ReliableOrdered);

        }

        /****************** Server only methods *******************************/

        private void JumpToMap(ushort nMap)
        {

            _map = nMap;

            SetMap();

            // Send new map packet to all except our client
            foreach (ServerPlayer p in _playerManager)
            {
                if (p.Id!=0)
                    SendToPeer(p.AssociatedPeer, PacketType.NewMap, _currentMapData);

                // and reset each player for new level
                p.NewLevelReset();
                _playerManager.PlayerStates[p.player].Active = true;
            }

            // Tell our client to set the new map
            _clientLogic.OnNewMap(_currentMapData);

        }
        private void SetMap()
        {
            _levelSet.SetMap(_map);

            // We can now be sure that the new map is active in our client so...
            Level currentLevel = _levelSet.GetCurrentLevel();

            _currentMapData = new NewMapPacket { isCustom = _levelSet.IsCustomLevel, nMap = _map };
            if(_levelSet.IsCustomLevel)
            {
                _currentMapData.SetCustomMap(currentLevel.GetMapArray(),
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
                                                    width, isHorizontal);

                 /*   if ((x+objectMapArray.xOffset>-2 && x+objectMapArray.xOffset < 2) ||
                        (y+objectMapArray.yOffset>-2 && y+objectMapArray.yOffset < 2))
                    {
                        Debug.Log($"[S] Object processed at:'{x+objectMapArray.xOffset+0.5f}','{y + objectMapArray.yOffset + 0.5f}' - created:'{worldObject!=null}'");
                    }*/

                    if (worldObject != null)
                    {
                        if(!_objectManager.AddWorldObject(worldObject))
                        {
                            Debug.Log($"[S] Failed to add object of type '{worldObject.Type}' at:'{worldObject.Position.x}','{worldObject.Position.y}");
                        }
                    }

                }

            // for debugging:
            // create chest object next to spawn location
       /*     var chestPosition = currentLevel.GetSpawnPoint()+new Vector2(2.0f, 2.0f);
            Chest chestObject = (Chest)_objectManager.CreateWorldObject(ObjectType.Chest, new WorldVector(chestPosition.x, chestPosition.y));
            if (chestObject != null)
            {
                chestObject.AddItem(PlayerBagItem.Health);
                chestObject.AddItem(PlayerBagItem.Bomb);
                chestObject.AddItem(PlayerBagItem.KeyBlue);
                chestObject.AddItem(PlayerBagItem.KeyGreen);
                chestObject.AddItem(PlayerBagItem.Health);
                _objectManager.AddWorldObject(chestObject);
            }*/
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
                case ObjectType.NPC_level1:
                case ObjectType.NPC_level2:
                case ObjectType.NPC_level3:
                    scoreFactor = ((NPC)worldObject).level;
                    break;
                case ObjectType.Gen_level1:
                case ObjectType.Gen_level2:
                case ObjectType.Gen_level3:
                    scoreFactor = ((Gen)worldObject).level;
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
                            var newNPC = (NPC)_objectManager.CreateWorldObject(ObjectType.NPC_level3, worldObject.Position);
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
                case ObjectType.NPC_level1:
                case ObjectType.NPC_level2:
                case ObjectType.NPC_level3:
                    damageFactor = ((NPC)worldObject).level;
                    break;
                case ObjectType.Gen_level1:
                case ObjectType.Gen_level2:
                case ObjectType.Gen_level3:
                    damageFactor = ((Gen)worldObject).level;
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
        public void OnSpawnMonster(object sender, SpawnMonsterEventArgs e)
        {
            Gen genObject = (Gen)_objectManager.GetById(e.generatorId);
            if (genObject == null)
                return;

            var newNPC = (NPC)_objectManager.CreateWorldObject(ObjectType.NPC_level1+genObject.level, e.position);
            newNPC.level=genObject.level;

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

        /*************** IEventListener methods ****************************************/


        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

            if (peer.Tag != null)
            {
                byte playerId = (byte)peer.Id;
                if (_playerManager.RemovePlayer(playerId))
                {
                    var plp = new PlayerLeftPacket { Id = (byte)peer.Id };
                    _netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType) packetType;
            switch (pt)
            {
                case PacketType.Movement:
                    OnInputReceived(reader, peer);
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader, peer);
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {

        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer.Tag != null)
            {
                var p = (ServerPlayer) peer.Tag;
                p.Ping = latency;
            }
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("dandelion0.1");
        }

    }
}