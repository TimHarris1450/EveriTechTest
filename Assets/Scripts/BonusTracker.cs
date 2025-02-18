using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class BonusTracker : MonoBehaviour
    {
        // reference to black hole 
        [SerializeField]
        private BlackHoleController _blackHole;
        // animators to play
        public List<Animator> Animators;
        // int for count
        public int BonusCount = 0;

        // add symbol to list
        public void AddSymbol(Animator anim)
        {
            Animators.Add(anim);
        }
        // increment the count
        public void Increment()
        {
            BonusCount += 1;
        }
        public List<Animator> GetAnimators()
        {
            return Animators;
        }
        // Method to check symbols when spinning stops
        public void CheckSymbol()
        {
            //if we have 3, stop all animations and play win
            if (BonusCount == 3)
            {
                foreach (Animator anim in new List<Animator>(Animators))
                {
                    anim.SetTrigger("win");
                    _blackHole.PlayBlackHole();
                }
                ResetSymbols();
            }
            // if we have 2, play hit and anticipation
            else if (BonusCount == 2)
            {
                foreach (Animator anim in new List<Animator>(Animators))
                {
                    anim.SetTrigger("anticipation");
                }
            }
        }

        // Reset the list
        private void ResetSymbols()
        {
            BonusCount = 0;
            Animators.Clear();
        }
    }
}

