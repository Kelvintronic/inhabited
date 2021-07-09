using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

namespace GameEngine
{
    public class UiController : MonoBehaviour
    {
        [SerializeField] private MainMenu _uiMainMenu;
        [SerializeField] private GameObject _uiObjectHUD;
        [SerializeField] private MapMenu _uiMapMenu;
        [SerializeField] private LevelMenu _levelMenu;


        [SerializeField] private ClientLogic _clientLogic;
        [SerializeField] private ServerLogic _serverLogic;
        [SerializeField] private InputField _ipField;
        [SerializeField] private InputField _nameField;
        [SerializeField] private Text _disconnectInfoField;
        [SerializeField] private Text _playerHelp;

        [SerializeField] private LevelSet _levelSet;
        [SerializeField] private FogOfWar _fogOfWar;

        [SerializeField] private Camera _mainCamera;

        public bool ConfineMouse;

        private bool _showMenu = false;
        private bool _showMap = false;


        // holds lock values to manage the Windows cursor
        private CursorLockMode _lockMode;

        private bool bMapMenuActive = false;
        private void Awake()
        {
            _ipField.text = NetUtils.GetLocalIp(LocalAddrType.IPv4);
            _nameField.text = "harry";

            _lockMode = CursorLockMode.None;
            Cursor.lockState = _lockMode;

        }


        private void Update()
        {

            // toggle main menu (and hide level menu if open)
            if(!_showMap)
                if (Keyboard.current.escapeKey.wasPressedThisFrame && _clientLogic.IsConnected())
                {
                    if (_levelMenu.IsVisible&&!_showMenu)
                        // Close only the level menu (but don't open the main menu)
                        _levelMenu.SetVisible(false);
                    else
                    {
                        // Close main menu (and level menu)
                        _showMenu = !_showMenu;
                        if (_levelMenu.IsVisible)
                            _levelMenu.SetVisible(false);
                        _uiMainMenu.SetVisible(_showMenu);
                        _clientLogic.DisableInput(_showMenu);
                        _playerHelp.gameObject.SetActive(!_showMenu);
                    }

                }
                else
                // toggle level menu
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    if(_serverLogic.IsStarted)
                    {
                        _levelMenu.SetVisible(!_levelMenu.IsVisible);
                        _clientLogic.DisableInput(_levelMenu.IsVisible);
                    }
                }

            if (_clientLogic.IsConnected())
            {
                if (ConfineMouse && !_uiMainMenu.IsVisible)
                    _lockMode = CursorLockMode.Confined;
                else
                    _lockMode = CursorLockMode.None;

                Cursor.lockState = _lockMode;

                if(Keyboard.current.tabKey.wasPressedThisFrame && !_showMenu)
                {
                    _showMap = !_showMap;
                    _clientLogic.DisableInput(_showMap);
                    _clientLogic.HidePlayer(_showMap);
                    _fogOfWar.LightExplored(_showMap);
                    _mainCamera.gameObject.SetActive(!_showMap);
                    _levelSet.mapCamera.gameObject.SetActive(_showMap);
                }
            }

        }

        public void OnHostClick()
        {
            _serverLogic.StartServer();
            _uiMainMenu.SetConnected(true);
            _uiMainMenu.SetVisible(false);
            _uiObjectHUD.SetActive(true);
            _clientLogic.NetConnect("localhost", _nameField.text, OnDisconnected);
            // show escape key message
            _playerHelp.gameObject.SetActive(true);
        }

   
        private void OnDisconnected(string info)
        {
            _showMenu = false;
            _uiMainMenu.SetConnected(false);
            _levelMenu.SetVisible(false);
            _uiMainMenu.SetVisible(true);
            _uiObjectHUD.SetActive(false);
            _disconnectInfoField.text = info;
            _showMap = false;
            _mainCamera.gameObject.SetActive(true);
            _levelSet.mapCamera.gameObject.SetActive(false);
            Cursor.visible = true;
        }

        public void OnConnectClick()
        {
            if(!_clientLogic.IsConnected())
            {
                _uiMainMenu.SetConnected(true);
                _uiMainMenu.SetVisible(false);
                _uiObjectHUD.SetActive(true);
                _clientLogic.NetConnect(_ipField.text, _nameField.text, OnDisconnected);

            }
            else
            {
                _clientLogic.Disconnect();
                if (_serverLogic.IsStarted)
                    _serverLogic.StopServer();
                _uiMainMenu.SetConnected(false);
                // show escape key message
                _playerHelp.gameObject.SetActive(true);
            }

        }

        public void OnLevelMenuGoButton()
        {
            _showMenu = false;
            _levelMenu.SetVisible(false);
            _uiMainMenu.SetVisible(false);
            _clientLogic.DisableInput(false);
            if (_levelMenu.ProposedLevel != _levelSet.CurrentLevelIndex)
                _serverLogic.JumpToLevel(_levelMenu.ProposedLevel);
        }

        public void OnLevelMenuBackButton()
        {
            _levelMenu.SetVisible(false);
            if(!_showMenu)
                _clientLogic.DisableInput(false);
        }

        public void OnMapMenu()
        {
            bMapMenuActive = !bMapMenuActive;
            if (bMapMenuActive)
            {
                // Turn on map menu
                _fogOfWar.Set(false);
                _uiMapMenu.SetVisible(true);
                _uiMainMenu.SetVisible(false);
            }
            else
            {
                // Turn off map menu
                 _fogOfWar.Set(true);
                _uiMapMenu.SetVisible(false);
                _uiMainMenu.SetVisible(true);
            }
        }

        public void OnTestLevel()
        {
            bMapMenuActive = false;

            // Turn off map menu
            _fogOfWar.Set(true);
            _uiMapMenu.SetVisible(false);

            // ensure custom level is set
            _levelSet.EnableCustomLevel(true);

            // host
            OnHostClick();
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}
