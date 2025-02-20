using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace Scripts
{
    public class SymbolAnimController : MonoBehaviour
    {
        // create an instance of the SymbolAnimController
        public static SymbolAnimController Instance;
        // awake method to set the instance
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        // play the hit animation
        public void PlayHit(Animator symbol)
        {
            symbol.GetComponentInChildren<Animator>().SetTrigger("hit");
        }
        // win animation
        public void PlayWin(List<Animator> symbol)
        {
            foreach (Animator anim in symbol)
            {
                anim.SetTrigger("win");
            }
        }
        // anticipation animation
        public void PlayAnticipation(List<Animator> symbol)
        {
            foreach (Animator anim in symbol)
            {
                anim.SetTrigger("anticipation");
            }
        }
        // idle animation
        public void PlayIdle(Animator symbol)
        {
            symbol.GetComponentInChildren<Animator>().SetTrigger("idle");
        }
    }
}