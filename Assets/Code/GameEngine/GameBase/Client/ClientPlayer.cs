using LiteNetLib;
using UnityEngine;

namespace GameEngine
{ 
    public class ClientPlayer : BasePlayer
    {
        private readonly ClientLogic _clientLogic;
        private readonly LiteRingBuffer<PlayerInputPacket> _predictionPlayerStates;
        private ServerState _lastServerState;
        private const int MaxStoredCommands = 60;
        private bool _firstStateReceived;
        private int _updateCount;

        private ActivateObjectPacket _activateCommand;
        private PickupObjectPacket _pickupCommand;

        private bool _hasSetActivate = false;
        private bool _hasSetPickup = false;

        public WorldVector _actualPosition;
        public WorldVector LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public int StoredCommands => _predictionPlayerStates.Count;

        public bool _bColliderChecked = false;
        public ClientPlayer(ClientLogic clientLogic, string name, JoinAcceptPacket jaPacket) : base(name, jaPacket.Id)
        {
            player = jaPacket.Player;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _activateCommand = new ActivateObjectPacket();
            _pickupCommand = new PickupObjectPacket();
            _pickupCommand.playerId = jaPacket.Id;
            _clientLogic = clientLogic;
        }

        public void ReceiveServerState(ServerState serverState, PlayerState ourState)
        {
            if (!_firstStateReceived)
            {
                if (serverState.LastProcessedCommand == 0)
                    return;
                _firstStateReceived = true;
            }

            // check if we have recieved the same state as last time
            // if so do nothing
            if (serverState.Tick == _lastServerState.Tick || 
                serverState.LastProcessedCommand == _lastServerState.LastProcessedCommand)
                return;

            _lastServerState = serverState;

            // get up to date Health and Score info from server
            _health = ourState.Health;
            _score = ourState.Score;
            _cash = ourState.Cash;

            // these are not acted on any more just used as old states for lerping if needed
            _position = ourState.Position;
            _rotation = ourState.Rotation;

            // if we have died clear input packet
            if(!IsAlive)
                SetInput(WorldVector.Zero, 0.0f, false);

            // if we have left the level this will be false
            _active = ourState.Active;

            _bag.Clear();
            foreach(var slot in ourState.Bag)
               _bag.Add(slot);

        }

        public override void Spawn(float x, float y)
        {
            base.Spawn(x,y);
        }

        public PlayerInputPacket SetInput(WorldVector velocity, float rotation, bool fire)
        {
            _nextCommand.Keys = 0;
            if(fire)
                _nextCommand.Keys |= MovementKeys.Fire;
            
            if (velocity.x < -0.5f)
                _nextCommand.Keys |= MovementKeys.Left;
            if (velocity.x > 0.5f)
                _nextCommand.Keys |= MovementKeys.Right;
            if (velocity.y < -0.5f)
                _nextCommand.Keys |= MovementKeys.Up;
            if (velocity.y > 0.5f)
                _nextCommand.Keys |= MovementKeys.Down;

            _nextCommand.Rotation = rotation;

            return _nextCommand;

        }

        public void SetActivate(int objectId, ObjectType objectType)
        {
             if(_activateTimer.IsTimeElapsed)
            {
                _hasSetActivate = true;
                _activateCommand.objectId = objectId;
                _activateCommand.type = objectType;
            }
        }

        public void SetPickup(int id)
        {
            _hasSetPickup = true;
            _pickupCommand.objectId = id;
        }

        public void SetCommandPosition(WorldVector position)
        {
            _nextCommand.Position = position;
        }

        public override void Update(float delta) 
        {
            base.Update(delta);

            if (!_active)
                return;

            LastPosition = _position;
            LastRotation = _rotation;
            
            _nextCommand.Id = (ushort)((_nextCommand.Id + 1) % NetworkGeneral.MaxGameSequence);
            _nextCommand.ServerTick = _lastServerState.Tick;

            // send movement packet
            _clientLogic.SendPacketSerializable(PacketType.Movement, _nextCommand, DeliveryMethod.Unreliable);

            if (_hasSetActivate)
            {
                _clientLogic.SendPacket(_activateCommand,DeliveryMethod.ReliableOrdered);
                _activateTimer.Reset();
            }
            _hasSetActivate = false;

            if (_hasSetPickup)
            {
                _clientLogic.SendPacket(_pickupCommand, DeliveryMethod.ReliableOrdered);
            }
            _hasSetPickup = false;

        }
    }
}