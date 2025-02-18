using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class BonusTracker : MonoBehaviour
    {
        // event to trigger when all 3 symbols are up
        public delegate void BonusEvent();
        // animators to play
        public List<Animator> Animators;
        // int for count
        public int BonusCount = 0;
        // Machine Controller for events
        [SerializeField]
        private MachineController machineController;

        // setup listener for events
        private void OnEnable()
        {
            machineController.OnReelsStop.AddListener(CheckSymbol); 
        }
        private void OnDisable()
        {
            machineController.OnReelsStop.RemoveListener(CheckSymbol); 
        }
        
        // increment method
        public void Increment()
        {
            BonusCount++;
        }
        // reset method
        public void Reset()
        {
            BonusCount = 0;
        }
        // add symbol to list
        public void AddSymbol(Animator anim)
        {
            Animators.Add(anim);
            CheckSymbol();
        }
        public List<Animator> GetAnimators()
        {
            return Animators;
        }
        // Method to check symbols when spinning stops
        public void CheckSymbol()
        {
            BonusCount++;
            //if we have 3, stop all animations and play win
            if (BonusCount == 3)
            {
                foreach (Animator anim in Animators)
                {
                    anim.SetTrigger("hit");
                    ResetSymbols();
                    // trigger black hole with event
                }
            }
            // if we have 2, play hit and anticipation
            else if (BonusCount == 2)
            {
                foreach (Animator anim in Animators)
                {
                    anim.SetTrigger("anticipation");
                }
            }
            // if we have 1, play hit on that one
            else if (BonusCount == 1)
            {
                foreach (Animator anim in Animators)
                {
                    anim.SetTrigger("hit");
                }
            }

        }

        // Reset the list
        private void ResetSymbols()
        {
            foreach (Animator anim in Animators)
            {
                anim.SetTrigger("win");
            }
            BonusCount = 0;
            Animators.Clear();
        }
    }
}

