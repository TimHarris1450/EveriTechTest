using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Scripts
{
    public class MachineController : MonoBehaviour
    {

        [SerializeField]
        public GameObject Reels;
        [SerializeField]
        private float _wait = 2f;
        private float _defaultWait;
        [SerializeField]
        private float _waitIncrement = 0.75f;
        [SerializeField]
        private bool _spinning = false;

        // method for starting and stopping spin routine
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
        // coroutine to incrementally start reels
        private IEnumerator StartSpin()
        {
            _defaultWait = _wait;
            _spinning = true;

            // int to increment for reel spin
            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                // set the animator controller state
                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("spin");
                Animator anim = Reels.transform.GetChild(i).GetChild(2).GetChild(0).GetComponent<Animator>();
                _wait -= _waitIncrement;
                yield return new WaitForSeconds(_wait);
            }
            // extra delay for automatic stopping
            yield return new WaitForSeconds(_defaultWait);
            StartCoroutine(StopSpin());
        }
        // stopping the reels
        private IEnumerator StopSpin()
        {
            BonusTracker _bonusTracker = FindObjectOfType<BonusTracker>();
            _wait = _defaultWait;
            // int to increment for reel spin
            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                // set the animator controller state
                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("stop");
                _wait += 0.5f;
                yield return new WaitForSeconds(_wait);
                _bonusTracker.CheckSymbol();
            }
            yield return new WaitForSeconds(_wait);
            Stopped();

        }
        // method to call when reels stop
        public void Stopped()
        {
            // set spinning to false
            _spinning = false;
        }
    }
}

