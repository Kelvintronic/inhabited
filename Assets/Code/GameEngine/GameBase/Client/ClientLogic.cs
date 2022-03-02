using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Random = System.Random;

namespace GameEngine
{

    public class ObjectEventArg : EventArgs
    {
        public int objetId;
        public ObjectType type;
    }

    public class ClientLogic : MonoBehaviour, INetEventListener
    {

        [SerializeField] private ClientPlayerView _clientPlayerViewPrefab;
        [SerializeField] private RemotePlayerView _remotePlayerViewPrefab;

        [SerializeField] private ServerLogic _serverLogic;
        [SerializeField] private Inventory _inventory;

        [SerializeField] private FogOfWar _fogOfWar;

        [SerializeField] private LevelSet _levelSet;

        // TODO: Move to HUD
        [SerializeField] private Text _debugText;
        [SerializeField] private Text _playerText;
        private bool _showDebugMessages = false;

        private PrefabStore _prefabStore;

        private ClientPlayerView _ourPlayerView;
        private ClientPlayer _ourPlayer;
        private Sprite[] _spriteArray;

        private Action<string> _onDisconnected;
        private string _playerHandle;

        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;

        private ServerState _cachedServerState;
        private WorldObjectState _cachedObjectState;
        private ushort _lastServerTick;
        private NetPeer _server;
        private ClientPlayerManager _playerManager;
        private ClientObjectManager _objectManager;
        private int _ping;

        private bool _isGameFull = false;

        public bool IsOldKeys;
        public static LogicTimer LogicTimer { get; private set; }
        public bool IsServer { get => _serverLogic != null && _serverLogic.IsStarted; }
        public ClientPlayerManager PlayerManager => _playerManager;

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _spriteArray = Resources.LoadAll<Sprite>("DandySprites");

            Random r = new Random();
            _cachedServerState = new ServerState();
            _cachedObjectState = new WorldObjectState();
            LogicTimer = new LogicTimer(OnLogicUpdate);
            _writer = new NetDataWriter();
            _playerManager = new ClientPlayerManager(this);
            _objectManager = new ClientObjectManager();
            //NPCManager = GetComponent<NPCManager>();
            _prefabStore = GetComponent<PrefabStore>();

            _packetProcessor = new NetPacketProcessor();
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetWorldVector());
            _packetProcessor.RegisterNestedType<PlayerState>(); // this allows PlayerState to be nested in the PlayerJoinedPacket
                                                                //  _packetProcessor.RegisterNestedType<NPCState>(); <-- not required as NPCState is never nested
            _packetProcessor.SubscribeReusable<PlayerJoinedPacket>(OnPlayerJoined);
            _packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
            _packetProcessor.SubscribeReusable<JoinRejectPacket>(OnJoinReject);
            _packetProcessor.SubscribeReusable<PlayerLeftPacket>(OnPlayerLeft);
            _packetProcessor.SubscribeReusable<SpawnPacket>(OnPlayerSpawnPacket);
            //            _packetProcessor.SubscribeReusable<NewMapPacket>(OnNewMap);
            _packetProcessor.SubscribeReusable<RemoveObjectPacket>(OnRemoveObject);
            _packetProcessor.SubscribeReusable<ActivateObjectPacket>(OnActivateObject);
            _packetProcessor.SubscribeReusable<ReleaseObjectLockPacket>(OnObjectUnlockPacket);
            _packetProcessor.SubscribeReusable<PlayerPositionCorrection>(OnPlayerPositionCorrection);
            _packetProcessor.SubscribeReusable<RevealAreaPacket>(OnRevealAreaPacket);
            _packetProcessor.SubscribeReusable<ShootPacket>(OnShootPacket);



            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = IPv6Mode.Disabled // false
            };
            _netManager.Start();

            _inventory.InventorySelect += OnInventorySelect;

        }

        public byte GetPlayerId()
        {
            return _ourPlayer.Id;
        }

        public void OnLogicUpdate()
        {
            _playerManager.LogicUpdate();
        }

        public void Update()
        {

            if (Keyboard.current.f12Key.wasPressedThisFrame)
                _showDebugMessages = !_showDebugMessages;

            if (_ourPlayerView != null)
            {
                // update invenory
                if (_ourPlayer != null)
                    _inventory.Update(_ourPlayer);
            }

            // hide inventory if we are dead
            if (_ourPlayer != null)
            {
                _inventory.gameObject.SetActive(_ourPlayer.IsAlive);
                if (!_ourPlayer.IsAlive)
                    _fogOfWar.Set(false);
            }

            _netManager.PollEvents();
            LogicTimer.Update();

            if (_showDebugMessages)
            {
                if (!_debugText.gameObject.activeInHierarchy)
                {
                    _debugText.gameObject.SetActive(true);
                    _playerText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (_debugText.gameObject.activeInHierarchy)
                {
                    _debugText.gameObject.SetActive(false);
                    _playerText.gameObject.SetActive(false);
                }
            }

            if (_ourPlayerView != null)
            {
                _playerText.text = _ourPlayerView.GetStatusText();
                _fogOfWar.Set(_ourPlayerView.IsFogOfWar);
            }
            else
                _playerText.text = "Player not instantiated";

            if (_playerManager.OurPlayer != null)
            {
                if (_serverLogic.IsStarted)
                {
                    _debugText.text = string.Format(
                            $"LastServerTick: {_lastServerTick}\n" +
                            $"StoredCommands: {_playerManager.OurPlayer.StoredCommands}\n" +
                            $"AStar: Q: {_serverLogic.GetAStarSearch().QueuedJobCount} " +
                            $"F:{_serverLogic.GetAStarSearch().FinishedJobCount}\n" +
                            $"Ping: {_ping}");

                }
                else
                {
                    _debugText.text = string.Format(
                            $"LastServerTick: {_lastServerTick}\n" +
                            $"StoredCommands: {_playerManager.OurPlayer.StoredCommands}\n" +
                            $"Ping: {_ping}");
                }

            }
            else
                _debugText.text = "Disconnected";
        }

        public void Disconnect()
        {
            _netManager.DisconnectAll();        // disconnect from server
            LogicTimer.Stop();                  // Stop LogicUpdate cycle
            _playerManager.RemoveAllPlayers();  // destroy local player objects
            _objectManager.RemoveAll();         // destroy all world objects
            _levelSet.SetMap(0);                // set lobby and destroy level object
            _fogOfWar.Clear();
            _server = null;
        }

        public void StopNetManager()
        {
            _netManager.Stop();
        }

        public void OnNewMap(MapPacket newMap)
        {
            Debug.Log("[C] Received NewMapPacket");

            // Destroy all objects from last map
            _objectManager.RemoveAll();

            if (!_serverLogic.IsStarted)
            {
                if (newMap.isCustom)
                    _levelSet.SetCustomMap(newMap.mapArray, newMap.spawnPoint, newMap.exitPoint);
                else
                    _levelSet.SetMap(newMap.nMap);
            }

            Vector2 spawnPoint = _levelSet.GetSpawnPoint(_ourPlayer.player);
            _ourPlayerView.gameObject.transform.position = spawnPoint;
            _ourPlayer.NewLevelReset();
            if (_fogOfWar != null)
                _fogOfWar.Clear();
        }

        public void OnRemoveObject(RemoveObjectPacket packet)
        {
            Debug.Log("[C] Received RemoveWorldObjectPacket");

            _objectManager.RemoveObject(packet.id);
        }
        public void OnPlayerSpawnPacket(SpawnPacket packet)
        {
            Debug.Log("[C] Received SpawnPacket");

            var player = _playerManager.GetById(packet.PlayerId);

            if (player.Id == _ourPlayer.Id)
                _ourPlayerView.Spawn(packet.x, packet.y);
            else
                player.Spawn(packet.x, packet.y);
        }

        public void OnActivateObject(ActivateObjectPacket packet)
        {
            Debug.Log("[C] Received ActivateObjectPacket");

            // we have been given permission to activate the object
            var worldObjectView = _objectManager.GetViewById(packet.objectId);
            worldObjectView.OnActivate(_ourPlayerView);
        }

        public void OnRevealAreaPacket(RevealAreaPacket packet)
        {
            Debug.Log("[C] Received Reveal Area Packet");

            Vector2 direction = new Vector2(Mathf.Cos(packet.direction + 90 * Mathf.Deg2Rad), Mathf.Sin(packet.direction + 90 * Mathf.Deg2Rad));
            Vector2 position = new Vector2(packet.position.x, packet.position.y);

            RaycastHit2D hit = Physics2D.Raycast(position, direction, 2.0f, LayerMask.GetMask("Mask"));
            if (hit.collider != null)
                hit.collider.gameObject.SetActive(false);

        }


        public void OnObjectUnlockPacket(ReleaseObjectLockPacket packet)
        {
            Debug.Log("[C] Received ReleaseObjectLockPacket");

            // the server has released the object lock
            var worldObjectView = _objectManager.GetViewById(packet.objectId);
            worldObjectView.OnRelease(_ourPlayerView);
        }

        public void OnObjectUnlock(object sender, ObjectEventArg e)
        {
            var packet = new ReleaseObjectLockPacket() { objectId = e.objetId };
            SendPacket(packet, DeliveryMethod.ReliableOrdered);
        }

        public void OnPlayerPositionCorrection(PlayerPositionCorrection packet)
        {
            Debug.Log("[C] Received Position Correction");

            _ourPlayerView.SetPositionCorrection(new Vector2(packet.x, packet.y));
        }

        private void OnInventorySelect(object sender, InventorySelectArg e)
        {
            var packet = new ActivateBagItemPacket() { slot = e.slot, drop = e.drop };
            SendPacket(packet, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Delegate: Called when a player takes an item from a chest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTakeItem(object sender, TakeItemArg e)
        {
            IObjectView view = (ChestView)sender;
            var packet = new TakeItemPacket() { chestId = view.GetId(), slotIndex = e.index };
            SendPacket(packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName}");

            // create player object
            var remotePlayer = new RemotePlayer(_playerManager, packet.UserName, packet);

            // Get spawnpoint from level and apply to player
            Vector2 spawnPoint = _levelSet.GetSpawnPoint(remotePlayer.player);
            remotePlayer.Spawn(spawnPoint.x, spawnPoint.y);

            // create view object
            var view = RemotePlayerView.Create(_remotePlayerViewPrefab, remotePlayer, _spriteArray[GetPlayerSpriteIndex(remotePlayer.player)]);

            // add to player manager for server updates
            _playerManager.AddPlayer(remotePlayer, view);
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            // Note: Server holds maxPlayer value and will only accept if
            // the additional player does not exceed this value.

            _lastServerTick = packet.ServerTick;

            // set Map (only if we are not the server)
            if(!IsServer)
                _levelSet.SetMap(packet.Map);

            // create new player
            _ourPlayer = new ClientPlayer(this, _playerHandle, packet);

            Debug.Log("[C] Join accept. Received player id: " + _ourPlayer.Name);

            // Get spawnpoint from level and apply to player
            Vector2 spawnPoint = _levelSet.GetSpawnPoint(_ourPlayer.player);
            _ourPlayer.Spawn(spawnPoint.x, spawnPoint.y);

            // instantiate view
            _ourPlayerView = ClientPlayerView.Create(this, _clientPlayerViewPrefab, _ourPlayer, _spriteArray[GetPlayerSpriteIndex(_ourPlayer.player)]);

            // add player and view to playermanager (for server updates)
            _playerManager.AddClientPlayer(_ourPlayer, _ourPlayerView);
        }

        private void OnJoinReject(JoinRejectPacket packet)
        {
            // Player has been rejected from jopining
            _lastServerTick = packet.ServerTick;
            _isGameFull = true;
            _netManager.DisconnectAll();
        }

        private void OnServerState()
        {
            //skip duplicate or old because we received that packet unreliably
            if (NetworkGeneral.SeqDiff(_cachedServerState.Tick, _lastServerTick) <= 0)
                return;
            _lastServerTick = _cachedServerState.Tick;

            _playerManager.ApplyServerState(ref _cachedServerState);

        }

        private void OnWorldObjectState()
        {
            for (int i = 0; i < _cachedObjectState.worldObjectCount; i++)
            {
                var view = _objectManager.GetViewById(_cachedObjectState.worldObjects[i].Id);
                if (view != null)
                {
                    // Update if exists
                    view.Update(_cachedObjectState.worldObjects[i], _cachedObjectState.tick);
                }
                else
                {
                    // or add if not
                    switch (_cachedObjectState.worldObjects[i].Type)
                    {
                        case ObjectType.DoorBlue:
                        case ObjectType.DoorRed:
                        case ObjectType.DoorGreen:
                        case ObjectType.Door:
                        case ObjectType.HiddenDoor:
                            view = DoorView.Create(_prefabStore.doorObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.KeyBlue:
                        case ObjectType.KeyRed:
                        case ObjectType.KeyGreen:
                            view = KeyView.Create(_prefabStore.keyObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.Health:
                            view = HealthView.Create(_prefabStore.healthObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.Heart:
                            view = HeartView.Create(_prefabStore.heartObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.Bomb:
                            view = BombView.Create(_prefabStore.bombObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.Cash:
                            view = CashView.Create(_prefabStore.cashObjectPrefab, _cachedObjectState.worldObjects[i]);
                            break;
                        case ObjectType.FalseWall:
                            view = GenericView.Create(_prefabStore.genericObjectPrefab, _cachedObjectState.worldObjects[i], _spriteArray[0]);
                            break;
                        case ObjectType.NPCBug:
                        case ObjectType.NPCTrader:
                        case ObjectType.NPCMercenary:
                            var npcView = NPCView.Create(_prefabStore.npcObjectPrefab, _cachedObjectState.worldObjects[i], _serverLogic.IsStarted);
                            if (_serverLogic.IsStarted)
                            {
                                npcView.Monster.Attack += _serverLogic.OnMonsterAttack;
                                npcView.Monster.EyesOn += _serverLogic.OnMonsterWatching;
                            }
                            view = npcView;
                            break;
                        case ObjectType.BugNest:
                            var genView = BugNestView.Create(_prefabStore.genObjectPrefab, _cachedObjectState.worldObjects[i], _serverLogic.IsStarted);
                            if (_serverLogic.IsStarted)
                            {
                                var generator = genView.GetComponent<Generator>();
                                generator.Spawn += _serverLogic.OnSpawnBug;
                            }
                            view = genView;
                            break;
                        case ObjectType.Chest:
                            view = ChestView.Create(_prefabStore.chestObjectPrefab, _cachedObjectState.worldObjects[i]);
                            ((ChestView)view).ObjectUnlockEvent += OnObjectUnlock;
                            ((ChestView)view).TakeItemEvent += OnTakeItem;
                            break;
                        case ObjectType.Conveyor:
                            view = ConveyorView.Create(_prefabStore.conveyorObjectPrefab, _cachedObjectState.worldObjects[i], _serverLogic.IsStarted);
                            break;
                    }
                    if (view != null)
                        _objectManager.AddWorldObject(_cachedObjectState.worldObjects[i], view);
                }
            }
        }


        private void OnPlayerLeft(PlayerLeftPacket packet)
        {
            var player = _playerManager.RemovePlayer(packet.Id);
            if (player != null)
                Debug.Log($"[C] Player quit: {player.Name}");
        }

        private void OnShootPacket(ShootPacket shootData)
        {
            GameObject gameObject = null;

            // is shot from an NPC or a player?
            if (shootData.IsNPCShooter)
            {
                var objectView = _objectManager.GetViewById(shootData.ShooterId);
                if (objectView != null)
                    gameObject = objectView.GetGameObject();
            }
            else
            {
                var playerView = _playerManager.GetViewById(shootData.ShooterId);
                if (playerView != null)
                    gameObject = playerView.GetGameObject();
            }

            if (gameObject != null)
            {
                // does the gameObject have a gun component?
                var gun = gameObject.GetComponent<Gun>();
                if (gun != null)
                    gun.Shoot(IsServer, shootData, _serverLogic.OnBoltHit);
            }
        }

        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            _server = peer;

            SendPacket(new JoinPacket { UserName = _playerHandle }, DeliveryMethod.ReliableOrdered);
            LogicTimer.Start();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // clean up
            Disconnect();

            if (_isGameFull)
            {
                _onDisconnected("Game is full");
                _onDisconnected = null;
                return;
            }

            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
            if (_onDisconnected != null)
            {
                _onDisconnected(disconnectInfo.Reason.ToString());
                _onDisconnected = null;
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Spawn:
                    break;
                case PacketType.WorldObjectState:
                    _cachedObjectState.Deserialize(reader);
                    OnWorldObjectState();
                    break;
                case PacketType.ServerState:
                    _cachedServerState.Deserialize(reader);
                    OnServerState();
                    break;
                case PacketType.NewMap:
                    MapPacket newMap = new MapPacket();
                    newMap.Deserialize(reader);
                    OnNewMap(newMap);
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader);
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
            _ping = latency;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        public void Connect(string ip, Action<string> onDisconnected)
        {
            _onDisconnected = onDisconnected;
            _netManager.Connect(ip, 10515, "dandelion0.1");
        }

        public bool IsConnected()
        {
            return _netManager.ConnectedPeersCount > 0;
        }

        private int GetPlayerSpriteIndex(int nPlayer)
        {
            return nPlayer + 3;
        }

        private void OnDestroy()
        {
            StopNetManager();
        }

        private Action<string> notifyDisconnected;
        public void NetConnect(string ip, string handle, Action<string> onDisconnected)
        {
            if (handle.Length > 15)
                _playerHandle = handle.Substring(0, 15);
            else
                _playerHandle = handle;
            notifyDisconnected = onDisconnected;
            Connect(ip, OnDisconnected);
        }

        private void OnDisconnected(string info)
        {
            notifyDisconnected(info);
        }

        public void DisableInput(bool isInputDisabled)
        {
            if (_ourPlayerView != null)
                _ourPlayerView.DisableInput(isInputDisabled);
        }

        public void HidePlayer(bool isHidden)
        {
            if (_ourPlayerView != null)
                _ourPlayerView.Hide(isHidden);
        }
    }
}