using UnityEngine;

namespace GameEngine
{
    public interface IPlayerView
    {
        void Destroy();

        GameObject Shoot(bool isServer);

        WorldVector GetActualPosition();

        byte GetId();

        void SetActive(bool bActive);

    }

    public interface IObjectView
    {
        void Destroy();
        void SetActive(bool isActive);
        void Update(WorldObject worldObject, ushort tick);
        ObjectType GetObjectType();
        int GetId();

        /// <summary>
        /// Server activation method. This method should only be called by 
        /// ClientLogic on receipt of an ActivateObjectPacket
        /// </summary>
        /// <param name="playerView"></param>
        void OnActivate(IPlayerView playerView);

        /// <summary>
        /// Server player release method. This method should only be called by
        /// ClientLogic on the receipt of a ReleaseObjectLockPacket.
        /// </summary>
        /// <param name="playerView"></param>
        void OnRelease(IPlayerView playerView);

    }
}
