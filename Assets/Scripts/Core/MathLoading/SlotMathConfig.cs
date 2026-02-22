using System;
using System.IO;
using Scripts.Core.Math;
using UnityEngine;

namespace Scripts.Core.MathLoading
{
    [CreateAssetMenu(fileName = "SlotMathConfig", menuName = "Slot/Math Config")]
    public class SlotMathConfig : ScriptableObject
    {
        [SerializeField]
        private string _xlsxRelativePath = "Math/SlotMathTemplate.xlsx";

        [SerializeField]
        private SlotMathRuntimeAsset _runtimeAsset;

        [SerializeField]
        private bool _preferRuntimeAsset;

        public string XlsxRelativePath
        {
            get => _xlsxRelativePath;
            set => _xlsxRelativePath = value;
        }

        public SlotMathRuntimeAsset RuntimeAsset
        {
            get => _runtimeAsset;
            set => _runtimeAsset = value;
        }

        public bool PreferRuntimeAsset
        {
            get => _preferRuntimeAsset;
            set => _preferRuntimeAsset = value;
        }

        public string ResolveXlsxPath()
        {
            return Path.IsPathRooted(_xlsxRelativePath)
                ? _xlsxRelativePath
                : Path.Combine(Application.streamingAssetsPath, _xlsxRelativePath);
        }

        public SlotMathModel LoadMathModel()
        {
            if (_preferRuntimeAsset && _runtimeAsset != null)
            {
                SlotMathModel cachedModel = _runtimeAsset.LoadModel();
                if (cachedModel != null)
                {
                    return cachedModel;
                }
            }

            return SlotMathLoader.LoadFromXlsx(ResolveXlsxPath());
        }

        public void SetXlsxPathFromAbsolute(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                _xlsxRelativePath = string.Empty;
                return;
            }

            string normalizedStreamingAssets = Path.GetFullPath(Application.streamingAssetsPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedAbsolute = Path.GetFullPath(absolutePath);

            if (normalizedAbsolute.StartsWith(normalizedStreamingAssets, StringComparison.OrdinalIgnoreCase))
            {
                string relative = normalizedAbsolute.Substring(normalizedStreamingAssets.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                _xlsxRelativePath = relative.Replace(Path.DirectorySeparatorChar, '/');
                return;
            }

            _xlsxRelativePath = normalizedAbsolute;
        }
    }
}
