using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Scripts
{
    public class MachineController : MonoBehaviour
    {
        // Event for reels to stop
        public UnityEvent OnReelsStop;
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
            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                // set the animator controller state
                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("spin");
                _wait -= _waitIncrememnt;
                yield return new WaitForSeconds(_wait);
            }
            // extra delay for automatic stopping
            yield return new WaitForSeconds(_wait);
            StartCoroutine(StopSpin());
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
                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("stop");
                _wait -= _waitIncrememnt + 0.2f;
                yield return new WaitForSeconds(_wait);
            }
            yield return new WaitForSeconds(0.5f);
            Stopped();
        }
        // method to call when reels stop
        public void Stopped()
        {
            // set spinning to false
            _spinning = false;
            // invoke the OnReelsStop event
            OnReelsStop?.Invoke();
        }
    }
}

