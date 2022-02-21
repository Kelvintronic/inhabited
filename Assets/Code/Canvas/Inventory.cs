using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameEngine;

public interface IInventoryItem
{
    string Name { get; }
    Sprite Image { get; }
}

public class InventorySelectArg : EventArgs
{
    public int slot;
    public bool drop;
}

public class Inventory : MonoBehaviour
{

    private const int MaxSlots = 5;
    [SerializeField] private InventorySlot[] _slots;
    [SerializeField] private Text _cashText;

    private int _highlightSlot = 0;

    public event EventHandler<InventorySelectArg> InventorySelect;

    private void Start()
    {

    }

    private void Update()
    {
        int slot = -1;

        // slot keys
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            slot = 0;
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            slot = 1;
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            slot = 2;
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            slot = 3;
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
            slot = 4;

        if (slot != -1)
            if (InventorySelect != null)
                InventorySelect.Invoke(this, new InventorySelectArg { slot = slot, drop = Input.GetKey(KeyCode.LeftShift) });

        // item keys
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            slot = FindItem(PlayerBagItem.Bomb);
        else if (Keyboard.current.rKey.wasPressedThisFrame)
            slot = FindItem(PlayerBagItem.Health);

        if (slot != -1)
            if (InventorySelect != null)
                InventorySelect.Invoke(this, new InventorySelectArg { slot = slot, drop = false });

    }

    // Mouse Click
    public void OnInventorySlotClick()
    {
        var button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
    }

    public void Update(ClientPlayer player)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (i < player.Bag.Count)
            {
                _slots[i].SetItem(player.Bag[i].type, player.Bag[i].count);
            }
            else
                _slots[i].SetItem(PlayerBagItem.Lint);
        }

        _cashText.text = "$" + player.Cash; 
    }

    private int FindItem(PlayerBagItem item)
    {
        for(int i=0;i<MaxSlots;i++)
        {
            if (_slots[i].Item == item)
                return i;
        }
        return -1;
    }
}
