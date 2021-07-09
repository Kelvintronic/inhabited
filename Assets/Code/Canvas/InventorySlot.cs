using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameEngine;

public class InventorySlot : MonoBehaviour
{
    private Image _borderImage;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Text _itemCountText;
    private Sprite[] _spriteArray;
    private PlayerBagItem _item;
    public PlayerBagItem Item => _item;

    void Awake()
    {
        _borderImage = GetComponent<Image>();
        _spriteArray = Resources.LoadAll<Sprite>("DandyObjectSprites");
        _itemCountText.gameObject.SetActive(false);
        _item = PlayerBagItem.Lint;
    }

    public void SetHighlight(bool isHighlighted)
    {
        if(isHighlighted)
            _borderImage.color = new Color(0xFF, 0x00, 0x00);
        else
            _borderImage.color = new Color(0x89, 0x89, 0x89);

    }

    public void SetItem(PlayerBagItem item, int count = 1)
    {
        _itemImage.enabled = true;
        _item = item;
        switch (item)
        {
            case PlayerBagItem.Bomb:
                _itemImage.sprite = _spriteArray[3];
                break;
            case PlayerBagItem.KeyBlue:
                _itemImage.sprite = _spriteArray[2];
                break;
            case PlayerBagItem.KeyGreen:
                _itemImage.sprite = _spriteArray[1];
                break;
            case PlayerBagItem.KeyRed:
                _itemImage.sprite = _spriteArray[0];
                break;
            case PlayerBagItem.Health:
                _itemImage.sprite = _spriteArray[8];
                break;
            default:
                _itemImage.enabled = false;
                _item = PlayerBagItem.Lint;
                break;
        }

        if (count > 1)
        {
            _itemCountText.text = count.ToString();
            _itemCountText.gameObject.SetActive(true);
        }
        else
            _itemCountText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
