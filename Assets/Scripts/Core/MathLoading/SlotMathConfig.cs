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

        public SlotMathModel LoadMathModel()
        {
            string path = Path.IsPathRooted(_xlsxRelativePath)
                ? _xlsxRelativePath
                : Path.Combine(Application.streamingAssetsPath, _xlsxRelativePath);

            return SlotMathLoader.LoadFromXlsx(path);
        }
    }
}
