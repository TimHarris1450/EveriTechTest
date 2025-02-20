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
        // Method to check symbols when spinning stops
        public void CheckSymbol()
        {
            Debug.Log("Checking symbols");
            StartCoroutine(CheckSymbolCoroutine());
        }
        private IEnumerator CheckSymbolCoroutine()
        {
            // get the symbol anim controller
            SymbolAnimController symAnims = FindObjectOfType<SymbolAnimController>();  
            // check how many landed bonus symbols and designate animations
            switch (BonusCount)
            {
                //if we have 3, stop all animations and play win
                case 3:
                    // play win animation
                    foreach (Animator anim in Animators)
                    {
                        symAnims.PlayWin(Animators);
                    }
                    yield return new WaitForSeconds(0.85f);
                    _blackHole.PlayBlackHole();
                    ResetSymbols();
                    break;
                // if we have 2, play hit and anticipation
                case 2:
                    foreach (Animator anim in Animators)
                    {
                        symAnims.PlayAnticipation(Animators);
                    }
                    break;                
            }       
            yield return new WaitForSeconds(1f);
        }
        // Reset the list
        private void ResetSymbols()
        {
            BonusCount = 0;
            Animators.Clear();
        }
    }
}

