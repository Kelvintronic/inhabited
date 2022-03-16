
namespace GameEngine
{   
    public class RemotePlayer : BasePlayer
    {
        private readonly LiteRingBuffer<PlayerState> _buffer = new LiteRingBuffer<PlayerState>(30);
        private float _receivedTime;
        private float _timer;
        private const float BufferTime = 0.1f; //100 milliseconds

        public RemotePlayer(ClientPlayerManager manager, string name, PlayerJoinedPacket pjPacket) : base(name, pjPacket.InitialPlayerState.Id)
        {
            player = pjPacket.Player;
            _position = pjPacket.InitialPlayerState.Position;
            _health = pjPacket.Health;
            _score = pjPacket.Score;
            _rotation = pjPacket.InitialPlayerState.Rotation;
        }

        private bool _isMoving = false;
        public bool IsMoving => _isMoving;

        /// <summary>
        /// Allows positioning of a player anywhere is world space
        /// i.e. bypasses lerp
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public override void Spawn(float x, float y)
        {
            _buffer.FastClear();
            base.Spawn(x,y); 
        }

        // this function smoothes out the remote player movement by slowing
        // down the player movement to keep pace with the network delay
        // Instead of applying the new position immediately it waits until it has
        // two positions and moves the player based on time somewhere inbetween
        // the two positions. It then removes the first item in the buffer and 
        // does it again next pass with the next position recieved.
        public void UpdatePosition(float delta)
        {
            if (_buffer.Count < 2)
                return;
            var dataA = _buffer[0];
            var dataB = _buffer[1];
            
            float lerpTime = NetworkGeneral.SeqDiff(dataB.Tick, dataA.Tick)*LogicTimer.FixedDelta;
            float t = _timer / lerpTime;
            _position = WorldVector.Lerp(dataA.Position, dataB.Position, t);
            if (dataA.Position == dataB.Position)
                _isMoving = false;
            else
                _isMoving = true;
            _timer += delta;
            if (_timer > lerpTime)
            {
                _receivedTime -= lerpTime;
                _buffer.RemoveFromStart(1);
                _timer -= lerpTime;
            }
        }

        // Add position in state to ring buffer for use by UpdatePosition()
        public void OnPlayerState(PlayerState state)
        {
            _active = state.Active;
            _rotation = state.Rotation;
            _health = state.Health;
            _score = state.Score;

            if(_buffer.Count>0)
            {
                //old command
                int diff = NetworkGeneral.SeqDiff(state.Tick, _buffer.Last.Tick);
                if (diff <= 0)
                    return;

                _receivedTime += diff * LogicTimer.FixedDelta;
                if (_buffer.IsFull)
                {
                    //                Debug.LogWarning("[C] Remote: Something happened");
                    //Lag?
                    _receivedTime = 0f;
                    _buffer.FastClear();
                }
            }
            _buffer.Add(state);
        }

    }
}