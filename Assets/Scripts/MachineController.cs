using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineController : MonoBehaviour
{
    // List of Reels
    [SerializeField]
    public GameObject Reels;
    [SerializeField]
    private float _wait = 1f;
    private float _defaultWait;
    [SerializeField]
    private float _waitIncrememnt = 0.5f;
    [SerializeField]
    private bool _spinning = false;

    // Method for starting and stopping spin routine
    public void SpinCheck()
    {
        switch (_spinning)
        {
            case true:        
                StartCoroutine(StopSpin());
                break;
            case false:
                StartCoroutine(StartSpin());
                break;
        }
    }
    // Coroutine to incrementally start reels
    private IEnumerator StartSpin()
    {
        _defaultWait = _wait;
        _spinning = true;
        // int to increment for reel spin
        for(int i = 0; i < Reels.transform.childCount; i++)
        {
            // set the animator controller state
            transform.GetChild(0).GetChild(i).GetComponent<Animator>().SetTrigger("spin");
            _wait *= _waitIncrememnt;
            yield return new WaitForSeconds(_wait);
        }
        StopCoroutine(StartSpin());
    }
    // stopping the reels
    private IEnumerator StopSpin()
    {
        _wait = _defaultWait;
        // int to increment for reel spin
        for (int i = 0; i < Reels.transform.childCount; i++)
        {
            // set the animator controller state
            transform.GetChild(0).GetChild(i).GetComponent<Animator>().SetTrigger("stop");
            _wait *= _waitIncrememnt;
            yield return new WaitForSeconds(_wait);
        }
        _spinning = false;
    }
}
