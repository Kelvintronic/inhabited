using System.Collections.Generic;

namespace GameEngine
{
    public class ServerPlayerManager : BasePlayerManager
    {
        private readonly ServerPlayer[] _players;
        
        public readonly PlayerState[] PlayerStates;
        private int _playersCount;
        private const int MaxPlayers = 4;
        public override int Count => _playersCount;
        public int nPlayerSlots => MaxPlayers;

        private INetSender _netSender;
        public ServerPlayerManager(INetSender netSender)
        {
            _netSender = netSender;
            _players = new ServerPlayer[MaxPlayers];
            PlayerStates = new PlayerState[MaxPlayers];
        }

        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            for(int i=0;i<MaxPlayers;i++)
            {
                if (_players[i] != null)
                    yield return _players[i];
            }
        }
    
        public int GetInactivePlayers()
        {
            int inactivePlayers = 0;
            foreach (ServerPlayer p in this)
                if (!p.IsActive)
                    inactivePlayers++;
            return inactivePlayers;
        }

        public ServerPlayer GetById(byte id)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i] == null)
                    continue;

                if (_players[i].Id == id)
                    return _players[i];
            }
            return null;
        }

        public bool AddPlayer(ServerPlayer player)
        {
            if (_playersCount == MaxPlayers)
                return false; ;

            // check if the player exists already
            // if so update player
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i]!=null&&_players[i].Id == player.Id)
                {
                    player.player = i; // number used to assign sprite
                    _players[i] = player;
                    return true;
                }
            }

            // find first available slot
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i]==null)
                {
                    player.player = i; // number used to assign sprite
                    _players[i] = player;
                    _playersCount++;
                    return true;
                }
            }

            return false;
        }

        public override void LogicUpdate()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i] == null)
                    continue;
                var p = _players[i];

                // flag player if they have died since last update
                p.deadThisTick = !p.IsAlive && p.NetworkState.Health > 0;

                p.Update(LogicTimer.FixedDelta);
                PlayerStates[i] = p.NetworkState;
            }
        }

        public bool RemovePlayer(byte playerId)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i] == null)
                    continue;

                if (_players[i].Id == playerId)
                {
                    _playersCount--;
                    _players[i] = null;
                    return true;
                }
            }
            return false;
        }

        public void RemoveAllPlayers()
        {
            for (int i = 0; i < MaxPlayers; i++)
                    _players[i] = null;
            _playersCount = 0;
        }

        public bool ResurrectNextDeadPlayer(WorldVector position)
        {
            // TODO: Add time of death and resurrect longest dead player
            foreach (ServerPlayer player in this)
            {
                if (!player.IsAlive)
                {
                    player.AddHealth(100);
                    _netSender.SendToAll(new SpawnPacket { PlayerId = player.Id, x = position.x, y = position.y });
                    return true;
                }
            }
            return false;
        }

        public void TelePortPlayer(byte playerId, WorldVector position)
        {
            _netSender.SendToAll(new SpawnPacket { PlayerId = playerId, x=position.x, y=position.y });
        }
    }
}
