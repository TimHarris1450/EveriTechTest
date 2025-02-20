using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Scripts
{
    public class ImageSetter : MonoBehaviour
    {
        // List of Prefabs to replace
        public List<GameObject> Symbols;
        [SerializeField]
        private GameObject _bonusSymbol;
        [SerializeField]
        BonusTracker _bonusTracker;

        // Method to set random image, int to determine which child
        public void RandomImage(int childIndex)
        {
            // create reference
            GameObject child = transform.GetChild(childIndex).gameObject;
            // destroy current child
            Destroy(child.transform.GetChild(0).gameObject);
            // exclude the bonus symbol from the random selection
            List<GameObject> nonBonusSymbols = Symbols.FindAll(symbol => symbol != _bonusSymbol);
            int rando = Random.Range(0, nonBonusSymbols.Count);
            // make a new child
            GameObject newChild = Instantiate(nonBonusSymbols[rando]);
            // add to reel
            newChild.transform.SetParent(child.transform, false);
        }
        // Set the bonus symbol to the 2nd row for activation
        public void SetBonusSymbol()
        {
            // check if we are the first or last reel
            if (name == "Reel_1" || name == "Reel_5")
            {
                Debug.Log($"{name} is not eligible for a bonus symbol.");
                return;
            }
            Debug.Log($"{name} is setting a bonus symbol.");
            StartCoroutine(SetSymbol());
        }
        
        private IEnumerator SetSymbol()
        {
            // reference to the symbol anim controller
            SymbolAnimController sac = FindObjectOfType<SymbolAnimController>();
            // destroy the child and replace with bonus
            Destroy(transform.GetChild(2).GetChild(0).gameObject);
            // create new symbol (Yes I know it should be a pool, but this is a small project)
            GameObject newChild = Instantiate(Symbols[0]);
            // reference animator
            Animator anim = newChild.GetComponent<Animator>();
            // add to bonus tracker
            _bonusTracker.AddSymbol(newChild.GetComponent<Animator>());
            // increment the bonus tracker
            _bonusTracker.Increment();
            // set the parent
            newChild.transform.SetParent(transform.GetChild(2), false);
            yield return new WaitForSeconds(0.5f);
            sac.PlayHit(newChild.GetComponent<Animator>());
            yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && !anim.IsInTransition(0));
        }
    }
}

