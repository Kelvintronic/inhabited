using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class HealthBar : MonoBehaviour
    {
        private Transform _bar;
        private bool _flashing = false;
        [SerializeField] private float _timeOn = 0.1f;
        [SerializeField] private float _timeOff = 0.25f;
        [SerializeField] private float _flashThreshold = 0.4f;
        private float _clock;

        private void Start()
        {
            _bar = transform.Find("Bar");
            _clock = Time.time;
        }

        private void Update()
        {
            if (_bar.localScale.x < _flashThreshold)
            {
                var timer = Time.time - _clock;

                if (_flashing)
                {
                    if (timer > _timeOn)
                    {
                        SetColor(Color.red);
                        _flashing = false;
                        _clock = Time.time;
                    }
                }
                else
                {
                    if (timer > _timeOn + _timeOff)
                    {
                        SetColor(Color.white);
                        _flashing = true;
                        _clock = Time.time;
                    }
                }
            }
            else
            {
                SetColor(Color.green);
            }
        }

        public void SetSize(float sizeNormalised)
        {
            _bar.localScale = new Vector3(sizeNormalised, 1f);
        }

        public void SetColor(Color color)
        {
            _bar.Find("BarSprite").GetComponent<SpriteRenderer>().color = color;
        }
    }
}