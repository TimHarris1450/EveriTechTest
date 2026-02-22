using System;
using System.Collections.Generic;
using System.Linq;
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
            _bindings = prefabs?
                .Where(prefab => prefab != null)
                .GroupBy(prefab => prefab.name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new SymbolPrefabBinding
                {
                    PrefabKey = group.Key,
                    Prefab = group.First()
                })
                .ToList() ?? new List<SymbolPrefabBinding>();

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
