using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class FollowPlayer : MonoBehaviour
    {
        private Vector3 offset;
        private GameObject player;

        void Start()
        {
            offset = transform.position;
        }

        void LateUpdate()
        {
            if (player == null)
            {
                player = GameObject.Find("ClientPlayer(Clone)");
            }
            else
            {
                //transform.position = player.transform.position;// + offset;
                transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            }
        }
    }
}