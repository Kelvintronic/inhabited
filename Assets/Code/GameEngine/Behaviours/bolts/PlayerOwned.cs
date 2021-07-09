using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class PlayerOwned : MonoBehaviour
    {
        public byte PlayerId { get; set; }

        public string PlayerName { get; set; }

        public bool IsRemote { get; set; }
    }
}