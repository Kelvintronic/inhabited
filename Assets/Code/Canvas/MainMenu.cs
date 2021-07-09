using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Input Objects")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _connectButton;
    [SerializeField] private Button _controlMenuButton;
    [SerializeField] private InputField _ipField;
    [SerializeField] private InputField _usernameField;
    [SerializeField] private Button _mapMenuButton;
    [SerializeField] private Button _quitButton;

    [Header("Game Objects")]
    [SerializeField] private OptionsMenu _optionsMenu;

    private bool _isVisible = false;
    [HideInInspector] public bool IsVisible => _isVisible;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        gameObject.SetActive(isVisible);
        if (!_isVisible&&_optionsMenu.IsVisible)
            _optionsMenu.SetVisible(false);
    }

    public void OnOptionsMenuButton()
    {
        SetVisible(false);
        _optionsMenu.SetVisible(true);
    }

    public void SetConnected(bool isConnected)
    {
        if(isConnected)
        {
            _hostButton.interactable = false;
            _connectButton.GetComponentInChildren<Text>().text = "Disconnect";
            _ipField.interactable = false;
            _usernameField.interactable = false;
            _mapMenuButton.interactable = false;
        }
        else
        {
            _hostButton.interactable = true;
            _connectButton.GetComponentInChildren<Text>().text = "Connect";
            _ipField.interactable = true;
            _usernameField.interactable = true;
            _mapMenuButton.interactable = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
