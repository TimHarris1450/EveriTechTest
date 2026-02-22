using System;
using Scripts.Core.Math;
using UnityEngine;

namespace Scripts.Core.MathLoading
{
    [CreateAssetMenu(fileName = "SlotMathRuntimeAsset", menuName = "Slot/Math Runtime Asset")]
    public class SlotMathRuntimeAsset : ScriptableObject
    {
        [SerializeField]
        private string _sourcePath;

        [SerializeField]
        private string _sourceHash;

        [SerializeField]
        [TextArea(8, 24)]
        private string _serializedMathJson;

        [SerializeField]
        private string _generatedUtc;

        public string SourcePath => _sourcePath;
        public string SourceHash => _sourceHash;
        public string GeneratedUtc => _generatedUtc;

        public SlotMathModel LoadModel()
        {
            return string.IsNullOrWhiteSpace(_serializedMathJson)
                ? null
                : JsonUtility.FromJson<SlotMathModel>(_serializedMathJson);
        }

        public void SetSerializedMath(string sourcePath, string sourceHash, SlotMathModel model)
        {
            _sourcePath = sourcePath;
            _sourceHash = sourceHash;
            _generatedUtc = DateTime.UtcNow.ToString("O");
            _serializedMathJson = model == null ? string.Empty : JsonUtility.ToJson(model);
        }
    }
}
