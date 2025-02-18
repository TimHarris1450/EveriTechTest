using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class BlackHoleController : MonoBehaviour
    {
        bool flipFlop = false;
        // play black hole animation
        public void PlayBlackHole()
        {
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

        }
    }
}

