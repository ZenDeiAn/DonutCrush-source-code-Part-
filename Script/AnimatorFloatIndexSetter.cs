using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorFloatIndexSetter : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private int index;

    private void OnEnable()
    {
        GetComponent<Animator>().SetFloat(key, index);
    }
}
