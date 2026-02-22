using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Scripts.Core.Math;
using Scripts.Core.MathLoading;
using UnityEditor;
using UnityEngine;

namespace Scripts.Editor.SlotTools
{
    [CustomEditor(typeof(SlotMathConfig))]
    public class SlotMathConfigInspector : UnityEditor.Editor
    {
        private string _summary;
        private string _loadError;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SlotMathConfig config = (SlotMathConfig)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_runtimeAsset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_preferRuntimeAsset"));

            DrawSourcePicker(config);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Math"))
                {
                    SlotMathValidationWindow.Open(config);
                }

                if (GUILayout.Button("Generate Runtime Asset"))
                {
                    GenerateRuntimeAsset(config);
                }

                if (GUILayout.Button("Run Simulation"))
                {
                    SlotSimulationWindow.Open(config);
                }
            }

            DrawSummary();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSourcePicker(SlotMathConfig config)
        {
            EditorGUILayout.LabelField("Math Source (.xlsx)", EditorStyles.boldLabel);
            string currentPath = config.ResolveXlsxPath();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(currentPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    string selected = EditorUtility.OpenFilePanel("Select Slot Math Workbook", Application.streamingAssetsPath, "xlsx");
                    if (!string.IsNullOrWhiteSpace(selected))
                    {
                        Undo.RecordObject(config, "Set slot math xlsx source");
                        config.SetXlsxPathFromAbsolute(selected);
                        EditorUtility.SetDirty(config);
                        RefreshSummary(config);
                    }
                }
            }

            if (GUILayout.Button("Reload Summary"))
            {
                RefreshSummary(config);
            }
        }

        private void DrawSummary()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status Summary", EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(_loadError))
            {
                EditorGUILayout.HelpBox(_loadError, MessageType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_summary))
            {
                EditorGUILayout.HelpBox("No summary loaded. Click 'Reload Summary'.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(_summary, MessageType.None);
        }

        private void RefreshSummary(SlotMathConfig config)
        {
            try
            {
                string path = config.ResolveXlsxPath();
                SlotMathModel loadedModel = SlotMathLoader.LoadFromXlsx(path);
                _loadError = null;
                _summary = BuildSummary(path, loadedModel);
            }
            catch (Exception ex)
            {
                _summary = null;
                _loadError = ex.Message;
            }
        }

        private static string BuildSummary(string path, SlotMathModel model)
        {
            string[] reelLengths = model.Reels
                .OrderBy(reel => reel.ReelIndex)
                .Select(reel => $"R{reel.ReelIndex}:{reel.OrderedSymbolIds.Count}")
                .ToArray();

            return string.Join("\n", new[]
            {
                $"Source: {path}",
                $"Hash (SHA256): {ComputeSha256(path)}",
                $"Symbols: {model.Symbols.Count}",
                $"Reel lengths: {(reelLengths.Length == 0 ? "none" : string.Join(", ", reelLengths))}",
                $"Paytable rows: {model.Paytable.Count}"
            });
        }

        private static string ComputeSha256(string path)
        {
            if (!File.Exists(path))
            {
                return "missing";
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(File.ReadAllBytes(path));
            StringBuilder builder = new(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        private static void GenerateRuntimeAsset(SlotMathConfig config)
        {
            try
            {
                string sourcePath = config.ResolveXlsxPath();
                SlotMathModel model = SlotMathLoader.LoadFromXlsx(sourcePath);
                SlotMathRuntimeAsset runtimeAsset = config.RuntimeAsset;

                if (runtimeAsset == null)
                {
                    string destination = EditorUtility.SaveFilePanelInProject(
                        "Create Slot Math Runtime Asset",
                        "SlotMathRuntimeAsset",
                        "asset",
                        "Choose where to save the generated runtime asset.");
                    if (string.IsNullOrWhiteSpace(destination))
                    {
                        return;
                    }

                    runtimeAsset = CreateInstance<SlotMathRuntimeAsset>();
                    AssetDatabase.CreateAsset(runtimeAsset, destination);
                    config.RuntimeAsset = runtimeAsset;
                }

                Undo.RecordObject(runtimeAsset, "Generate slot math runtime asset");
                runtimeAsset.SetSerializedMath(sourcePath, ComputeSha256(sourcePath), model);
                EditorUtility.SetDirty(runtimeAsset);

                Undo.RecordObject(config, "Assign slot runtime asset");
                config.PreferRuntimeAsset = true;
                EditorUtility.SetDirty(config);

                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Runtime Asset Generated", "Slot math runtime asset generated successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
            }
        }
    }
}
