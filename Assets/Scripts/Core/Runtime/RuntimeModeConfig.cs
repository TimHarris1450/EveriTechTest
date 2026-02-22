using UnityEngine;

namespace Scripts.Core.Runtime
{
    [CreateAssetMenu(fileName = "RuntimeModeConfig", menuName = "Slot/Runtime Mode Config")]
    public class RuntimeModeConfig : ScriptableObject
    {
        [SerializeField]
        private bool _isProductionMode;

        [SerializeField]
        private bool _allowLegacyFallbackInDevelopment;

        public bool IsProductionMode => _isProductionMode;
        public bool AllowLegacyFallbackInDevelopment => _allowLegacyFallbackInDevelopment;
    }
}
