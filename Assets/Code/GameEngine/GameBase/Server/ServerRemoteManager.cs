using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;


namespace GameEngine
{
    public interface INetSender
    {
        void SendToAll<T>(PacketType type, T packet) where T : struct, INetSerializable;
        void SendToAll<T>(T packet) where T : class, new();
        void SendToPeer<T>(NetPeer peer, PacketType type, T packet) where T : struct, INetSerializable;
        void SendToPeer<T>(NetPeer peer, T packet) where T : class, new();
        NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable;
        NetDataWriter WritePacket<T>(T packet) where T : class, new();

    }
    public class ServerRemoteManager : INetEventListener, INetSender
    {
        private NetManager _netManager;
        private NetPacketProcessor _packetProcessor;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();

        private IServerData _serverData;
        private ServerPlayerManager _playerManager => _serverData.GetPlayerManager();
        private ServerObjectManager _objectManager => _serverData.GetObjectManager();
        private ushort _serverTick => _serverData.GetServerTick();
        private MapPacket _currentMapPacket => _serverData.GetMapPacket();

        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        private ActivateObjectPacket _cachedActivateCommand = new ActivateObjectPacket();

        private int _nextMap;
        public int NextMap => _nextMap;

        public ServerRemoteManager(IServerData serverData)
        {
            _serverData = serverData;

            _packetProcessor = new NetPacketProcessor();

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

        }

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
            var ja = new JoinAcceptPacket { Id = player.Id, ServerTick = _serverTick, Player = player.player, Map = _currentMapPacket.nMap};
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
            if (player.Id != 0)
                SendToPeer(peer, PacketType.NewMap, _currentMapPacket);

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
                    //WorldVector dir = new WorldVector(MathFloat.Cos(player.Rotation), MathFloat.Sin(player.Rotation));

                    ShootPacket sp = new ShootPacket
                    {
                        ShooterId = player.Id,
                        IsNPCShooter = false,
                        Direction = player.Rotation,
                        DamageFactor = 1,
                      //  CommandId = player.LastProcessedCommandId,
                        //ServerTick = _serverTick
                    };

                    SendToAll(sp);
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
                return;

            if (!worldObject.IsActive)
                return;

            // if object is found
            if (player.ApplyActivate(activatePacket))
            {
                // if activation is successfull do server action
                switch (worldObject.Type)
                {
                    case ObjectType.ExitPoint:
                        _nextMap = worldObject.Flags;
                        break;
                    case ObjectType.DoorBlue:
                    case ObjectType.DoorRed:
                    case ObjectType.DoorGreen:
                    case ObjectType.Door:
                    case ObjectType.HiddenDoor:
                        _objectManager.RemoveObject(activatePacket.objectId);
                        // calculate direction for reveal area raycast
                        var raycastDirection = ((Door)(worldObject)).GetRaycastDirection(player.Position, player.Rotation);
                        SendToAll(new RevealAreaPacket { direction = raycastDirection, position = player.Position });
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


        public void Start() => _netManager.Start(10515);
        public void Stop() => _netManager.Stop();
        public void PollEvents() => _netManager.PollEvents();
        public bool IsRunning => _netManager.IsRunning;
        public void DisconnectAll() => _netManager.DisconnectAll();

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
            PacketType pt = (PacketType)packetType;
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
                var p = (ServerPlayer)peer.Tag;
                p.Ping = latency;
            }
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("dandelion0.1");
        }

    }
}
