using System.Collections;
using System.Collections.Generic;
using GameEngine.Search;
using LiteNetLib.Utils;
using UnityEngine;

namespace GameEngine
{
    public abstract class ServerWorldObject : WorldObject
    {
        protected int _speed = 0;          // movement
        protected GameTimer _updateTimer;   // reaction time
        protected bool _update;             // internal notification: client update required
        private bool _isLocked = false;
        protected ServerPlayer _lockPlayer;
        protected List<int> _destroyNotificationList;
        protected INotificationManager _notificationManager;

        public bool IsLocked => _isLocked;
        public IReadOnlyList<int> DestroyNotifications => _destroyNotificationList.AsReadOnly(); 

        // world info needed for AI
        protected MapArray _mapArray;
        protected IEnumerable<BasePlayer> _playerList;
        protected AStarSearch _aStarSearch;
        public ServerWorldObject(WorldVector position, float refreshRate=0.5f, INotificationManager manager = null) : base(position)
        {
            if(refreshRate<0.2f)
                _updateTimer = new GameTimer(0.2f);
            else
                _updateTimer = new GameTimer(refreshRate);

            _destroyNotificationList = new List<int>();
            _notificationManager = manager;
        }

        public virtual bool Update(float delta)
        {
            _updateTimer.UpdateAsCooldown(delta);

            // relase lock if player has died
            if(_lockPlayer!=null||IsLocked)
            {
                if (!_lockPlayer.IsAlive)
                    _isLocked = false;
            }

            if (_update)
            {
                // an internal method has called for a client update
                _update = false;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Restricts control of the object to a specific player.
        /// Override this to set any other data but remember to call the base method.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual bool Lock(ServerPlayer player)
        {
            if (_isLocked)
                return false;

            _lockPlayer = player;
            _isLocked = true;
            return true;
        }

        public virtual bool Unlock(ServerPlayer player)
        {
            if(player.Id==_lockPlayer.Id)
                _isLocked = false;
            return !_isLocked;
        }

        /// <summary>
        /// Must be called after the creation of any new server object
        /// if that object contains AI logic
        /// </summary>
        /// <param name="mapArray"></param>
        /// <param name="playerList"></param>
        public void SetGameReferences(ILevelData levelData, IEnumerable<BasePlayer> playerList)
        {
            _mapArray = levelData.GetMapArray();
            _aStarSearch = levelData.GetAStarSearch();
            _playerList = playerList;
        }

        public void AddDestroyNotification(int id)
        {
            if (!_destroyNotificationList.Contains(id))
            {
                Debug.Log("Notification added");
                _destroyNotificationList.Add(id);
            }
        }

        /// <summary>
        /// Called when object is removed from ServerObjectManager
        /// to clean up any object references in mapArray
        /// Override this if the object uses any other than standard
        /// cells in the mapArray. E.g. NPC for moving
        /// </summary>
        public virtual void Destroy()
        {
            if (_isInteractable)
                return;

            var celPos = _mapArray.GetCellVector(_position);

            // deal with the width of the object
            for (int i = 0; i < Width; i++)
            {
                _mapArray.SetCell(celPos, MapCell.Empty);
                if (IsHorizontal)
                    celPos.x++;
                else
                    celPos.y++;
            }
        }

        /// <summary>
        /// This is called by any object we have asked to let us know when it is destroyed
        /// Override this to capture this notification
        /// </summary>
        public virtual void DestroyNotification()
        {

        }
    }

}

