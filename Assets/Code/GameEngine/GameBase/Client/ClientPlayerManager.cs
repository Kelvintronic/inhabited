using System.Collections.Generic;

namespace GameEngine
{
    public struct PlayerHandler
    {
        public readonly BasePlayer Player;
        public readonly IPlayerView View;

        public PlayerHandler(BasePlayer player, IPlayerView view)
        {
            Player = player;
            View = view;
        }

        public void Update(float delta)
        {
            Player.Update(delta);
        }
    }

    public class ClientPlayerManager : BasePlayerManager
    {
        private readonly Dictionary<byte, PlayerHandler> _players;
        private readonly ClientLogic _clientLogic;
        private ClientPlayer _clientPlayer;

        public ClientPlayer OurPlayer => _clientPlayer;
        public override int Count => _players.Count;

        public ClientPlayerManager(ClientLogic clientLogic)
        {
            _clientLogic = clientLogic;
            _players = new Dictionary<byte, PlayerHandler>();
        }

        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            foreach (var ph in _players)
                yield return ph.Value.Player;
        }

        public void ApplyServerState(ref ServerState serverState)
        {
            for (int i = 0; i < serverState.PlayerStatesCount; i++)
            {
                var state = serverState.PlayerStates[i];
                if (!_players.TryGetValue(state.Id, out var handler))
                    return;

                if (handler.Player == _clientPlayer)
                {
                    _clientPlayer.ReceiveServerState(serverState, state);
                }
                else
                {
                    var rp = (RemotePlayer)handler.Player;
                    rp.OnPlayerState(state);
                }

                // activate or deactivate each player
                if (handler.Player.IsAlive)
                    handler.View.SetActive(handler.Player.IsActive);    // if inbetween levels
                else
                    handler.View.SetActive(false);                      // if dead


            }
        }

   /*     public override void OnShoot(BasePlayer from, WorldVector dir)
        {
            if (_players.TryGetValue(from.Id, out var handler))
            {
                handler.View.SpawnShot(dir);
            }
        }*/

        public BasePlayer GetById(byte id)
        {
            return _players.TryGetValue(id, out var ph) ? ph.Player : null;
        }

        public IPlayerView GetViewById(byte id)
        {
            return _players.TryGetValue(id, out var ph) ? ph.View : null;
        }

        public BasePlayer RemovePlayer(byte id)
        {
            if (_players.TryGetValue(id, out var handler))
            {
                _players.Remove(id);
                handler.View.Destroy();
            }
        
            return handler.Player;

        }

        public void RemoveAllPlayers()
        {
            foreach(var player in _players)
            {
                player.Value.View.Destroy();
            }
            _players.Clear();
        }

        public override void LogicUpdate()
        {
            foreach (var kv in _players)
            {
                kv.Value.Update(LogicTimer.FixedDelta);
            }
        }

        public void AddClientPlayer(ClientPlayer player, IPlayerView view)
        {
            _clientPlayer = player;
            _players.Add(player.Id, new PlayerHandler(player, view));
        }
        
        public void AddPlayer(RemotePlayer player, IPlayerView view)
        {
            _players.Add(player.Id, new PlayerHandler(player, view));
        }
    }
}