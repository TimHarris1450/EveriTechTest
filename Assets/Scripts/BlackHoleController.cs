using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class BlackHoleController : MonoBehaviour
    {
        // bool to check if playing
        private bool playing = false;
        // bool to check which animation to play
        private bool flipFlop = false;
        // Meter Value 
        [SerializeField]
        private MeterValue _meterValue;


        // play black hole animation
        public void PlayBlackHole()
        {
            if (!playing)
            {
                StartCoroutine(Transition());
            }
        }
        // background transition routine
        private IEnumerator Transition()
        {
            playing = true;
            yield return new WaitForSeconds(2f);
            // need to invert for transitions back and forth
            flipFlop = !flipFlop;
            // play animation
            switch (flipFlop)
            {
                case true:
                    GetComponent<Animator>().SetTrigger("play");
                    break;
                case false:
                    GetComponent<Animator>().SetTrigger("reverse");
                    break;
            }
            playing = false;
            yield return new WaitForSeconds(2f);
            _meterValue.CountUp();
        }
    }
}

