using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class FogOfWar : MonoBehaviour
{
    [SerializeField] private Camera _shadeCamera;
    [SerializeField] private Camera _lightCamera;
    [SerializeField] private GameObject _shadeImage;
    GameTimer resetDelayTimer = new GameTimer(1.0f);
    

    // Start is called before the first frame update
    void Start()
    {
//        mainCamera=transform.Find("FogOfWarMainCamera").GetComponent<Camera>();
//        secondCamera = transform.Find("FogOfWarSecondCamera").GetComponent<Camera>();
    }

    public void Clear()
    {
        _shadeCamera.clearFlags=CameraClearFlags.SolidColor;
        _shadeCamera.backgroundColor = Color.black;
        _lightCamera.clearFlags = CameraClearFlags.SolidColor;
        _lightCamera.backgroundColor = Color.black;
        resetDelayTimer.Reset();
    }

    public void Set(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    public void LightExplored(bool isLight)
    {
        _shadeImage.SetActive(!isLight);
    }

    // Update is called once per frame
    void Update()
    {
        resetDelayTimer.UpdateAsCooldown(Time.deltaTime);
        if (_shadeCamera.clearFlags == CameraClearFlags.SolidColor&&resetDelayTimer.IsTimeElapsed)
        {
                _shadeCamera.clearFlags = CameraClearFlags.Nothing;
                _lightCamera.clearFlags = CameraClearFlags.Nothing;
        }
    }
}
