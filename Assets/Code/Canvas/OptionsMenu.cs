using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameEngine;

public class OptionsMenu : MonoBehaviour
{
    [Header("Input Objects")]
    [SerializeField] private Button _1Button;
    [SerializeField] private Button _2Button;
    [SerializeField] private Toggle _destroyKeyToggle;
    [SerializeField] private Toggle _oldKeysToggle;
    [SerializeField] private Toggle _confineMouseToggle;
    [SerializeField] private Button _backButton;

    [Header("Game Objects")]
    [SerializeField] private ClientLogic _clientLogic;
    [SerializeField] private ServerLogic _serverLogic;
    [SerializeField] private MainMenu _mainMenu;
    [SerializeField] private UiController _uiController;
    [SerializeField] private GameObject _keyMap;

    private bool _isVisible = false;
    private bool _isKeyMapVisible = false;
    [HideInInspector] public bool IsVisible => _isVisible;

    // Start is called before the first frame update
    void Start()
    {
        _destroyKeyToggle.SetIsOnWithoutNotify(_serverLogic.IsDestroyKeyOnUse);
        _oldKeysToggle.SetIsOnWithoutNotify(_clientLogic.IsOldKeys);
        _confineMouseToggle.SetIsOnWithoutNotify(_uiController.ConfineMouse);
    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        if (_isKeyMapVisible)
            OnKeyMapBackButton();
        gameObject.SetActive(isVisible);
    }


    public void OnKeyMapBackButton()
    {
        _isKeyMapVisible = false;
        _keyMap.SetActive(false);

    }

    public void On1Button()
    {
        _isKeyMapVisible = true;
        _keyMap.SetActive(true);
    }
    public void On2Button()
    {
    }

    public void OnDestroyKeyToggle()
    {
        _serverLogic.IsDestroyKeyOnUse = !_serverLogic.IsDestroyKeyOnUse;
    }

    public void OnOldKeysToggle()
    {
        _clientLogic.IsOldKeys = !_clientLogic.IsOldKeys;
    }
    public void OnConfineMouse()
    {
        _uiController.ConfineMouse=!_uiController.ConfineMouse;
    }

    public void OnBackButton()
    {
        SetVisible(false);
        _mainMenu.SetVisible(true);
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
