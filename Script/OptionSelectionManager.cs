using System;
using System.Collections.Generic;
using Airpass.Language;
using Airpass.XRSports;
using UnityEngine;
using UnityEngine.UI;

public class OptionSelectionManager : MonoBehaviour
{
    [SerializeField] private Image img_title;
    [SerializeField] private Sprite optionButtonSelected;
    [SerializeField] private List<Button> optionButtons;
    private readonly List<Sprite> _optionButtonOriginalSprite = new();
    
    public void Btn_Back()
    {
        switch (XRSports.XRSportsType)
        {
            case XRSportsXR.TYPE:
            case XRSportsRK.TYPE:
                XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.title);
                break;
            
            case XRSportsIPC.TYPE:
            case XRSportsOB.TYPE:
                XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.lobby);
                break;
        }
    }

    public void Btn_Confirm()
    {
        XRSports.Option = GameManager.Instance.gameOption.ToString();
    }

    public void Btn_SetOption(int index)
    {
        GameManager.Instance.gameOption = (GameOption)index;
        XRSports.Option = GameManager.Instance.gameOption.ToString();
        UpdateInformation();
    }

    public void UpdateInformation()
    {
        if (_optionButtonOriginalSprite.Count == 0)
        {
            foreach (var button in optionButtons)
            {
                _optionButtonOriginalSprite.Add(button.image.sprite);
            }
        }

        int index = (int)GameManager.Instance.gameOption;
        for (int i = 0; i < optionButtons.Count; ++i)
        {
            optionButtons[i].image.sprite = i == index ? 
                optionButtonSelected : _optionButtonOriginalSprite[i];
        }
    }

    void OnEnable()
    {
        img_title.sprite = 
            (Sprite)LanguageManager.GetLanguageData("XRSportsUI", 
                $"Option_Title_{XRSports.XRSportsType}");
    }
}
