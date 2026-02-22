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

        public bool TryGetPrefab(string prefabKey, out GameObject prefab)
        {
            prefab = null;
            return !string.IsNullOrWhiteSpace(prefabKey)
                && _prefabsByKey != null
                && _prefabsByKey.TryGetValue(prefabKey, out prefab);
        }
    }
}
