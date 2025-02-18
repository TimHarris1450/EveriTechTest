using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Scripts
{
    public class MachineController : MonoBehaviour
    {
        // List of Reels
        [SerializeField]
        public GameObject Reels;
        [SerializeField]
        private float _wait = 2f;
        private float _defaultWait;
        [SerializeField]
        private float _waitIncrememnt = 0.75f;
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
                _wait += 0.5f;
                yield return new WaitForSeconds(_wait);
            }
            yield return new WaitForSeconds(0.5f);
            GetBonusSymbols();
            Stopped();
        }

        private void GetBonusSymbols()
        {
            // make a list of bonus symbols
            List<Animator> bonusSymbols = new List<Animator>();
            if (GetComponentInChildren<Transform>().name.Contains("Bonus"))
            {
                Debug.Log("Found Bonus");
            }
            Reels.transform.GetChild(0).GetComponent<Animator>().SetTrigger("hit");
        }
        // method to call when reels stop
        public void Stopped()
        {
            // set spinning to false
            _spinning = false;
        }
    }
}

