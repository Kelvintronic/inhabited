using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameEngine;
using System;


public class TakeItemArg : EventArgs
{
    public int index;
}

public class ChestView : MonoBehaviour, IObjectView
{
    [SerializeField] private GameObject _spriteOpen;
    [SerializeField] private GameObject _spriteClosed;
    [SerializeField] private ContentScroll _scroller;

    private WorldObject _worldObject;

    private ChestData _chestData;
    private ClientPlayerView _playerView;

    public event EventHandler<ObjectEventArg> ObjectUnlockEvent;
    public event EventHandler<TakeItemArg> TakeItemEvent;

    private GameTimer _contentSelectTimer = new GameTimer(0.2f);


    public static ChestView Create(ChestView prefab, WorldObject worldObject)
    {
        Quaternion rot = Quaternion.Euler(0f, 0, 0f);
        var obj = Instantiate(prefab, new Vector2(worldObject.Position.x, worldObject.Position.y), rot);
        obj._worldObject = worldObject;
        return obj;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Ensure chest is closed
        SetOpen(false);
        _scroller.ContentSelect += OnContentSelect;
        _chestData = (ChestData)_worldObject.GetDataObject();

     /*   _chestData = new ChestData();
        _chestData.AddItem(PlayerBagItem.Health);
        _chestData.AddItem(PlayerBagItem.Bomb);
        _chestData.AddItem(PlayerBagItem.KeyBlue);
        _chestData.AddItem(PlayerBagItem.KeyGreen);
        _chestData.AddItem(PlayerBagItem.Health);*/
    }

    // Update is called once per frame
    void Update()
    {
        _contentSelectTimer.UpdateAsCooldown(Time.deltaTime);

        // catch the excape key (incase the player decides to open the menu)
        if(Keyboard.current.escapeKey.wasPressedThisFrame && _scroller.isActiveAndEnabled)
        {
            // release object lock
            if (ObjectUnlockEvent != null)
                ObjectUnlockEvent.Invoke(this, new ObjectEventArg { objetId = _worldObject.Id, type = ObjectType.Chest });
            _scroller.gameObject.SetActive(false);  // hide scroll view
            SetOpen(false);                         // flip sprite to closed
        }

    }

    public void SetOpen(bool isOpen)
    {
        if (isOpen)
        {
            _spriteClosed.SetActive(false);
            _spriteOpen.SetActive(true);
        }
        else
        {
            _spriteClosed.SetActive(true);
            _spriteOpen.SetActive(false);
        }


    }

    private void OnContentSelect(object sender, ContentSelectArg e)
    {
        if(_contentSelectTimer.IsTimeElapsed)
        {
            _contentSelectTimer.Reset();
            if (!e.take)
            {
                _playerView.DisableInput(false);
                _scroller.gameObject.SetActive(false);
                if (ObjectUnlockEvent != null)
                    ObjectUnlockEvent.Invoke(this, new ObjectEventArg { objetId = _worldObject.Id, type = ObjectType.Chest });
            }
            else
            {
                if (TakeItemEvent != null)
                    TakeItemEvent.Invoke(this, new TakeItemArg { index = e.index });
            }
        }
    }


    ObjectType IObjectView.GetObjectType()
    {
        return ObjectType.Chest;
    }

    void IObjectView.OnActivate(IPlayerView playerView)
    {
        _playerView = (ClientPlayerView) playerView;
        _playerView.DisableInput(true);
        _scroller.Update(_chestData);
        _scroller.gameObject.SetActive(true);
    }

    void IObjectView.OnRelease(IPlayerView playerView)
    {
        _playerView = (ClientPlayerView)playerView;
        _playerView.DisableInput(false);
        _scroller.gameObject.SetActive(false);
    }

    void IObjectView.Destroy()
    {
        Destroy(gameObject);
    }

    void IObjectView.Update(WorldObject worldObject, ushort tick)
    {
        _worldObject = worldObject;
        _chestData = (ChestData)_worldObject.GetDataObject();
        if (_chestData.Items.Count > 0)
            _scroller.Update(_chestData);
        SetOpen(_chestData.isOpen);
    }

    int IObjectView.GetId()
    {
        return _worldObject.Id;
    }

    void IObjectView.SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

}
