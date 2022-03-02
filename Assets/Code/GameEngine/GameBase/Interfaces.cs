using UnityEngine;

namespace GameEngine
{
    public interface IPlayerView
    {
        void Destroy();
        void SetActive(bool bActive);
        byte GetId();
        GameObject GetGameObject();

        WorldVector GetActualPosition();

    }

    public interface IObjectView
    {
        void Destroy();
        void SetActive(bool isActive);
        int GetId();
        ObjectType GetObjectType();

        void Update(WorldObject worldObject, ushort tick);
        GameObject GetGameObject();

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
