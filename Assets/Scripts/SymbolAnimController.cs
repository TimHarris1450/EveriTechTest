using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace Scripts
{
    public class SymbolAnimController : MonoBehaviour
    {
        // symbol 2 child reference
        [SerializeField]
        private GameObject _symbol;
        // play the hit animation
        public void PlayHit()
        {
            _symbol.GetComponentInChildren<Animator>().SetTrigger("hit");
        }
    }
}