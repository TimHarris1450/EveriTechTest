using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Presentation
{
    public class SymbolRegistry : MonoBehaviour
    {
        [Serializable]
        public class SymbolPrefabBinding
        {
            public string PrefabKey;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<SymbolPrefabBinding> _bindings = new();

        private Dictionary<string, GameObject> _prefabsByKey;

        private void Awake()
        {
            RebuildLookupFromBindings();
        }

        public void BuildFromPrefabs(IEnumerable<GameObject> prefabs)
        {
            Dictionary<string, GameObject> merged = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _bindings.Count; i++)
            {
                SymbolPrefabBinding binding = _bindings[i];
                if (binding?.Prefab == null || string.IsNullOrWhiteSpace(binding.PrefabKey))
                {
                    continue;
                }

                if (!merged.ContainsKey(binding.PrefabKey))
                {
                    merged[binding.PrefabKey] = binding.Prefab;
                }
            }

            if (prefabs != null)
            {
                foreach (GameObject prefab in prefabs)
                {
                    if (prefab == null || string.IsNullOrWhiteSpace(prefab.name))
                    {
                        continue;
                    }

                    if (!merged.ContainsKey(prefab.name))
                    {
                        merged[prefab.name] = prefab;
                    }
                }
            }

            _bindings = new List<SymbolPrefabBinding>(merged.Count);
            foreach (KeyValuePair<string, GameObject> pair in merged)
            {
                _bindings.Add(new SymbolPrefabBinding
                {
                    PrefabKey = pair.Key,
                    Prefab = pair.Value
                });
            }

            RebuildLookupFromBindings();
        }

        public bool TryGetPrefab(string prefabKey, out GameObject prefab)
        {
            if (_prefabsByKey == null)
            {
                RebuildLookupFromBindings();
            }

            prefab = null;
            return !string.IsNullOrWhiteSpace(prefabKey)
                && _prefabsByKey.TryGetValue(prefabKey, out prefab);
        }

        private void RebuildLookupFromBindings()
        {
            _prefabsByKey = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            foreach (SymbolPrefabBinding binding in _bindings)
            {
                if (binding?.Prefab == null || string.IsNullOrWhiteSpace(binding.PrefabKey))
                {
                    continue;
                }

                _prefabsByKey[binding.PrefabKey] = binding.Prefab;
            }
        }
    }
}
