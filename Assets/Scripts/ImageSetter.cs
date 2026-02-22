using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField]
        private SymbolAnimController _symbolAnimController;
        [SerializeField]
        private bool _enableSymbolSwapPooling;

        private readonly Dictionary<int, int> _resolvedSymbolsByChild = new();
        private readonly Dictionary<int, SymbolData> _symbolDataById = new();
        private readonly Dictionary<int, GameObject> _prefabBySymbolId = new();
        private readonly Dictionary<GameObject, Queue<GameObject>> _symbolPoolByPrefab = new();
        private readonly Dictionary<int, GameObject> _prefabByInstanceId = new();

        private HashSet<int> _bonusEligibleReels = new();
        private int _reelIndex = -1;
        private GameObject _fallbackPrefab;

        public void ConfigureMath(SlotMathModel mathModel, int reelIndex)
        {
            _reelIndex = reelIndex;
            _symbolDataById.Clear();
            for (int i = 0; i < mathModel.Symbols.Count; i++)
            {
                SymbolData symbolData = mathModel.Symbols[i];
                _symbolDataById[symbolData.Id] = symbolData;
            }

            _bonusEligibleReels = new HashSet<int>(mathModel.Config.BonusEligibleReelIndices);
            RefreshSymbolLookup();
        }

        public void SetSymbolRegistry(SymbolRegistry symbolRegistry)
        {
            _symbolRegistry = symbolRegistry;
        }

        public void SetBonusTracker(BonusTracker bonusTracker)
        {
            _bonusTracker = bonusTracker;
        }

        public void SetSymbolAnimController(SymbolAnimController symbolAnimController)
        {
            _symbolAnimController = symbolAnimController;
        }

        public void SetSymbolSwapPoolingEnabled(bool enabled)
        {
            _enableSymbolSwapPooling = enabled;
        }

        public void RefreshSymbolLookup()
        {
            _prefabBySymbolId.Clear();
            _fallbackPrefab = null;

            for (int i = 0; i < Symbols.Count; i++)
            {
                GameObject symbol = Symbols[i];
                if (symbol == null || symbol == _bonusSymbol)
                {
                    continue;
                }

                _fallbackPrefab ??= symbol;
            }


            foreach (KeyValuePair<int, SymbolData> pair in _symbolDataById)
            {
                if (TryResolvePrefab(pair.Value, out GameObject prefab))
                {
                    _prefabBySymbolId[pair.Key] = prefab;
                    _fallbackPrefab ??= prefab;
                }
            }

            if (_fallbackPrefab == null && Symbols.Count > 0)
            {
                _fallbackPrefab = Symbols[0];
            }
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
            Transform child = transform.GetChild(childIndex);
            if (child.childCount > 0)
            {
                GameObject existing = child.GetChild(0).gameObject;
                ReleaseSymbol(existing);
            }

            int symbolId = GetResolvedSymbolId(childIndex);
            GameObject prefabToInstantiate = ResolvePrefabForSymbolId(symbolId);
            GameObject newChild = GetOrCreateSymbol(prefabToInstantiate);
            newChild.transform.SetParent(child, false);
            newChild.transform.localPosition = Vector3.zero;
            newChild.transform.localRotation = Quaternion.identity;
            newChild.transform.localScale = Vector3.one;
        }

        public void SetBonusSymbol()
        {
            if (!_bonusEligibleReels.Contains(_reelIndex))
            {
                Debug.Log($"Reel index {_reelIndex} is not eligible for a bonus symbol.");
                return;
            }

            StartCoroutine(SetSymbol());
        }

        private bool TryResolvePrefab(SymbolData symbolData, out GameObject prefab)
        {
            prefab = null;
            if (symbolData == null)
            {
                return false;
            }

            if (_symbolRegistry != null
                && _symbolRegistry.TryGetPrefab(symbolData.PrefabKey, out GameObject resolvedPrefab)
                && (!_bonusSymbol || resolvedPrefab != _bonusSymbol))
            {
                prefab = resolvedPrefab;
                return true;
            }

            for (int i = 0; i < Symbols.Count; i++)
            {
                GameObject symbol = Symbols[i];
                if (symbol == null || symbol == _bonusSymbol)
                {
                    continue;
                }

                if (string.Equals(symbol.name, symbolData.PrefabKey, StringComparison.OrdinalIgnoreCase))
                {
                    prefab = symbol;
                    return true;
                }
            }

            return false;
        }

        private GameObject ResolvePrefabForSymbolId(int symbolId)
        {
            if (_prefabBySymbolId.TryGetValue(symbolId, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }

            return _fallbackPrefab;
        }

        private GameObject GetOrCreateSymbol(GameObject prefab)
        {
            if (!_enableSymbolSwapPooling)
            {
                return Instantiate(prefab);
            }

            if (_symbolPoolByPrefab.TryGetValue(prefab, out Queue<GameObject> pool) && pool.Count > 0)
            {
                GameObject pooled = pool.Dequeue();
                pooled.SetActive(true);
                _prefabByInstanceId[pooled.GetInstanceID()] = prefab;
                return pooled;
            }

            GameObject created = Instantiate(prefab);
            _prefabByInstanceId[created.GetInstanceID()] = prefab;
            return created;
        }

        private void ReleaseSymbol(GameObject symbolInstance)
        {
            if (!_enableSymbolSwapPooling)
            {
                Destroy(symbolInstance);
                return;
            }

            int instanceId = symbolInstance.GetInstanceID();
            if (!_prefabByInstanceId.TryGetValue(instanceId, out GameObject prefab) || prefab == null)
            {
                Destroy(symbolInstance);
                return;
            }

            if (!_symbolPoolByPrefab.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                _symbolPoolByPrefab[prefab] = pool;
            }

            symbolInstance.SetActive(false);
            symbolInstance.transform.SetParent(transform, false);
            pool.Enqueue(symbolInstance);
        }

        private IEnumerator SetSymbol()
        {
            if (transform.GetChild(2).childCount > 0)
            {
                ReleaseSymbol(transform.GetChild(2).GetChild(0).gameObject);
            }

            GameObject newChild = GetOrCreateSymbol(Symbols[0]);
            Animator anim = newChild.GetComponent<Animator>();
            _bonusTracker?.AddSymbol(anim);
            _bonusTracker?.Increment();
            newChild.transform.SetParent(transform.GetChild(2), false);
            yield return new WaitForSeconds(0.5f);
            _symbolAnimController?.PlayHit(anim);
            yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && !anim.IsInTransition(0));
        }
    }
}
