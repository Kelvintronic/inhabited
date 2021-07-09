using LiteNetLib;

namespace GameEngine
{
    public class ServerPlayer : BasePlayer
    {
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }

        public bool deadThisTick = false;
        public ServerPlayer(string name, NetPeer peer) : base(name, (byte)peer.Id)
        {
            _active = true;
            peer.Tag = this;
            AssociatedPeer = peer;
            NetworkState = new PlayerState { Id = (byte)peer.Id, Active = true };
            NetworkState.SetBag(_bag);
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            // if we have recieved a packet command that comes before the last packet
            // processed do nothing
            if (NetworkGeneral.SeqDiff(command.Id, LastProcessedCommandId) <= 0)
                return;
            LastProcessedCommandId = command.Id;

            _position = command.Position;   // added by KJP
            _rotation = command.Rotation;   // added by KJP

            base.ApplyInput(command, delta);
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            NetworkState.Position = _position;
            NetworkState.Rotation = _rotation;
            NetworkState.Health = _health;
            NetworkState.Score = _score;
            NetworkState.Cash = _cash;
            NetworkState.Active = _active;
            NetworkState.Tick = LastProcessedCommandId;
            NetworkState.SetBag(_bag);
            
        }

        public override void SetActive(bool active)
        {
            NetworkState.Active = active;

            // if the following statement is removed the player will never reactivate
            // because in unity once we deactivate the player view object, the commandId never
            // changes because user imputs are deactivated. So the client won't process any new
            // server states unless the last command Id we send it is different from the one it holds
            LastProcessedCommandId++;

            base.SetActive(active);
        }

        public bool ApplyActivate(ActivateObjectPacket activatePacket)
        {
            if (_activateTimer.IsTimeElapsed)
            {
                _activateTimer.Reset();
                switch (activatePacket.type)
                {
                    case ObjectType.ExitPoint:
                        _active = false;
                        break;
                    case ObjectType.DoorRed:
                        return _bag.Exists(item => item.type == PlayerBagItem.KeyRed);
                    case ObjectType.DoorBlue:
                        return _bag.Exists(item => item.type == PlayerBagItem.KeyBlue);
                    case ObjectType.DoorGreen:
                        return _bag.Exists(item => item.type == PlayerBagItem.KeyGreen);
                }
                return true; // even if we don't need details accept activation
            }
            return false; // reject activation
        }

        public void SetPosition(WorldVector position)
        {
            _position = position;
        }
    }
}