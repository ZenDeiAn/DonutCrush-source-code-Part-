using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnableDisableObserver_ButtonActivation : EnableDisableObserver
{
    [SerializeField] private List<Button> buttons;
    [SerializeField] private EventSystemSelectedObjectUpdater essou;

    private void Awake()
    {
        onEnable.AddListener(() =>
        {
            foreach (var button in buttons)
            {
                button.enabled = false;
            }
        });
        onDisable.AddListener(() =>
        {
            foreach (var button in buttons)
            {
                button.enabled = true;
            }

            essou.SetSelected();
        });
        
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        foreach (var button in buttons)
        {
            button.enabled = true;
        }
    }
}
