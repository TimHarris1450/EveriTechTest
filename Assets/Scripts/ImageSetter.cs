using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scripts.Core.Math;
using Scripts.Presentation;
using UnityEngine;

namespace Scripts
{
    public class ImageSetter : MonoBehaviour
    {
        public List<GameObject> Symbols;
        [SerializeField]
        private GameObject _bonusSymbol;
        [SerializeField]
        private BonusTracker _bonusTracker;
        [SerializeField]
        private SymbolRegistry _symbolRegistry;

        private readonly Dictionary<int, int> _resolvedSymbolsByChild = new();
        private Dictionary<int, SymbolData> _symbolDataById = new();
        private HashSet<int> _bonusEligibleReels = new();
        private int _reelIndex = -1;

        public void ConfigureMath(SlotMathModel mathModel, int reelIndex)
        {
            _reelIndex = reelIndex;
            _symbolDataById = mathModel.Symbols.ToDictionary(symbol => symbol.Id);
            _bonusEligibleReels = new HashSet<int>(mathModel.Config.BonusEligibleReelIndices);
            if (_symbolRegistry == null)
            {
                _symbolRegistry = FindObjectOfType<SymbolRegistry>();
            }
        }

        public void SetSymbolRegistry(SymbolRegistry symbolRegistry)
        {
            _symbolRegistry = symbolRegistry;
        }

        public void SetResolvedStopSymbols(IReadOnlyList<int> stopSymbolIds)
        {
            _resolvedSymbolsByChild.Clear();
            for (int i = 0; i < stopSymbolIds.Count; i++)
            {
                _resolvedSymbolsByChild[i] = stopSymbolIds[i];
            }
        }


        public int GetResolvedSymbolId(int childIndex)
        {
            return _resolvedSymbolsByChild.TryGetValue(childIndex, out int symbolId) ? symbolId : -1;
        }

        public Animator GetSymbolAnimator(int childIndex)
        {
            if (childIndex < 0 || childIndex >= transform.childCount)
            {
                return null;
            }

            Transform symbolSlot = transform.GetChild(childIndex);
            return symbolSlot.childCount > 0 ? symbolSlot.GetChild(0).GetComponent<Animator>() : null;
        }

        public void RandomImage(int childIndex)
        {
            GameObject child = transform.GetChild(childIndex).gameObject;
            Destroy(child.transform.GetChild(0).gameObject);

            GameObject prefabToInstantiate = ResolvePrefabForChild(childIndex);
            GameObject newChild = Instantiate(prefabToInstantiate);
            newChild.transform.SetParent(child.transform, false);
        }

        public void SetBonusSymbol()
        {
            if (!_bonusEligibleReels.Contains(_reelIndex))
            {
                Debug.Log($"Reel index {_reelIndex} is not eligible for a bonus symbol.");
                return;
            }

            Debug.Log($"Reel index {_reelIndex} is setting a bonus symbol.");
            StartCoroutine(SetSymbol());
        }

        private GameObject ResolvePrefabForChild(int childIndex)
        {
            if (_resolvedSymbolsByChild.TryGetValue(childIndex, out int symbolId)
                && _symbolDataById.TryGetValue(symbolId, out SymbolData symbolData))
            {
                if (_symbolRegistry == null)
                {
                    _symbolRegistry = FindObjectOfType<SymbolRegistry>();
                }

                if (_symbolRegistry != null
                    && _symbolRegistry.TryGetPrefab(symbolData.PrefabKey, out GameObject resolvedPrefab)
                    && (!_bonusSymbol || resolvedPrefab != _bonusSymbol))
                {
                    return resolvedPrefab;
                }

                GameObject matchedFromSymbols = Symbols.Find(symbol => symbol != null
                    && symbol != _bonusSymbol
                    && string.Equals(symbol.name, symbolData.PrefabKey, StringComparison.OrdinalIgnoreCase));
                if (matchedFromSymbols != null)
                {
                    return matchedFromSymbols;
                }
            }

            List<GameObject> nonBonusSymbols = Symbols.FindAll(symbol => symbol != null && symbol != _bonusSymbol);
            return nonBonusSymbols.Count > 0 ? nonBonusSymbols[0] : Symbols[0];
        }

        private IEnumerator SetSymbol()
        {
            SymbolAnimController sac = FindObjectOfType<SymbolAnimController>();
            Destroy(transform.GetChild(2).GetChild(0).gameObject);
            GameObject newChild = Instantiate(Symbols[0]);
            Animator anim = newChild.GetComponent<Animator>();
            _bonusTracker.AddSymbol(newChild.GetComponent<Animator>());
            _bonusTracker.Increment();
            newChild.transform.SetParent(transform.GetChild(2), false);
            yield return new WaitForSeconds(0.5f);
            sac.PlayHit(newChild.GetComponent<Animator>());
            yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && !anim.IsInTransition(0));
        }
    }
}
