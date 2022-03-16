using GameEngine;
using GameEngine.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
     public class PlayerSpriteSet : MonoBehaviour
    {
        [SerializeField] private GameObject[] _sprites;

        private PlayerColour _colour;

        // Start is called before the first frame update
        private void Start()
        {

        }

        private void Update()
        {

        }

        public void SetSprite(PlayerColour colour)
        {
            if ((int)colour >= _sprites.Length)
                return;

            _colour = colour;

            for(int i=0; i<_sprites.Length; i++)
            {
                if (i == (int)colour)
                    _sprites[i].SetActive(true);
                else
                    _sprites[i].SetActive(false);
            }
        }

        public Animator GetAnimator()
        {
            if ((int)_colour >= _sprites.Length)
                return null;

            return _sprites[(int)_colour].GetComponent<Animator>();
        }
    }
}
