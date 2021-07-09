using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameEngine;


public class ContentSelectArg : EventArgs
{
    public int index;
    public bool take;
}

public class ContentScroll : MonoBehaviour
{

    [SerializeField] private InventorySlot[] _slots;

    private int _focusIndex = 0;
    private List<PlayerBagSlot> _content;
    public event EventHandler<ContentSelectArg> ContentSelect;


    private void Start()
    {
        _slots[1].SetHighlight(true);
    }


    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
            if(ContentSelect!=null)
                ContentSelect.Invoke(this, new ContentSelectArg { index = _focusIndex, take = false });

        if(Mouse.current.leftButton.wasPressedThisFrame)
            if (ContentSelect != null)
                ContentSelect.Invoke(this, new ContentSelectArg { index = _focusIndex, take = true });

        // capture mouse wheel
        var deltaWheel = Mouse.current.scroll.ReadValue(); 
        

        if (deltaWheel.y < 0)
            _focusIndex++;
        if (deltaWheel.y > 0)
            _focusIndex--;
        if (_focusIndex < 0)
            _focusIndex = 0;
        if (_focusIndex == _content.Count)
            _focusIndex--;

        if (_content.Count == 0)
            return;

        if (_focusIndex != 0)
            _slots[0].SetItem(_content[_focusIndex - 1].type, _content[_focusIndex - 1].count);
        else
            _slots[0].SetItem(PlayerBagItem.Lint);

        _slots[1].SetItem(_content[_focusIndex].type, _content[_focusIndex].count);

        if (_focusIndex != _content.Count - 1)
            _slots[2].SetItem(_content[_focusIndex + 1].type, _content[_focusIndex + 1].count);
        else
            _slots[2].SetItem(PlayerBagItem.Lint);

    }

    public void Update(ChestData data)
    {
        _content = data.Items;
        if (_focusIndex > _content.Count - 1)
            _focusIndex=_content.Count-1;
        for (int i = 0; i < 3; i++)
            _slots[i].SetItem(PlayerBagItem.Lint);
    }
}
