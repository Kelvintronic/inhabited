using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameEngine;

public class LevelMenu : MonoBehaviour
{
    [Header("Input Objects")]
    [SerializeField] private InputField _levelNumber;

    [Header("Game Objects")]
    [SerializeField] private LevelSet _levelSet;
    [SerializeField] private ServerLogic _serverLogic;

    private bool _isVisible = false;
    private int _proposedLevelIndex;

    public int ProposedLevel => _proposedLevelIndex;

    [HideInInspector] public bool IsVisible => _isVisible;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        if(_isVisible)
        {
            _proposedLevelIndex = _levelSet.CurrentLevelIndex;
            _levelNumber.text = _proposedLevelIndex.ToString();
        }
        gameObject.SetActive(isVisible);
    }


    public void OnNextButton()
    {
        _proposedLevelIndex++;
        if (_proposedLevelIndex >= _levelSet.NumberOfLevels)
            _proposedLevelIndex = 0;
        _levelNumber.text = _proposedLevelIndex.ToString();
    }

    public void OnPrevButton()
    {
        _proposedLevelIndex--;
        if (_proposedLevelIndex < 0)
            _proposedLevelIndex = _levelSet.NumberOfLevels-1;
        _levelNumber.text = _proposedLevelIndex.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
