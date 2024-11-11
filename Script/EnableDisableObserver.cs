using System;
using UnityEngine;
using UnityEngine.Events;

public class EnableDisableObserver : MonoBehaviour
{
    public bool subscriber;
    public string key;
    
    public UnityEvent onEnable;
    public UnityEvent onDisable;

    public static event Action<string> ObserverEnableEvent; 
    public static event Action<string> ObserverDisableEvent;

    private void SubscriberEnableCallback(string k)
    {
        if (key == k)
        {
            onEnable?.Invoke();
        }
    }
    
    private void SubscriberDisableCallback(string k)
    {
        if (key == k)
        {
            onDisable?.Invoke();
        }
    }

    protected virtual void OnEnable()
    {
        if (subscriber)
        {
            ObserverEnableEvent += SubscriberEnableCallback;
            ObserverDisableEvent += SubscriberDisableCallback;
        }
        else
        {
            ObserverEnableEvent?.Invoke(key);
        }
    }

    protected virtual void OnDisable()
    {
        if (subscriber)
        {
            ObserverEnableEvent -= SubscriberEnableCallback;
            ObserverDisableEvent -= SubscriberDisableCallback;
        }
        else
        {
            ObserverDisableEvent?.Invoke(key);
        }
    }
}
