using System;
using System.Collections.Generic;
using Scripts.Core.Math;
using Scripts.Core.MathLoading;
using Scripts.Core.Simulation;
using UnityEditor;
using UnityEngine;

namespace Scripts.Editor.SlotTools
{
    public class SlotSimulationWindow : EditorWindow
    {
        private SlotMathConfig _config;
        private int _spinCount = 10000;
        private int _seed = 12345;
        private bool _exportCsv = true;
        private Vector2 _scroll;
        private string _reportSummary = "No simulation run yet.";

        public static void Open(SlotMathConfig config)
        {
            SlotSimulationWindow window = GetWindow<SlotSimulationWindow>("Slot Simulation");
            window._config = config;
            window.Show();
        }

        private void OnGUI()
        {
            _config = (SlotMathConfig)EditorGUILayout.ObjectField("Config", _config, typeof(SlotMathConfig), false);
            _spinCount = EditorGUILayout.IntField("Spin Count", _spinCount);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _exportCsv = EditorGUILayout.Toggle("Export CSV", _exportCsv);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Assign a SlotMathConfig asset.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Run Simulation"))
            {
                RunSimulation();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox(_reportSummary, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void RunSimulation()
        {
            try
            {
                SlotMathModel model = _config.LoadMathModel();
                SlotSimulationRequest request = new()
                {
                    SpinCount = Mathf.Max(0, _spinCount),
                    Seed = _seed,
                    MathModel = model,
                    SelectedMathSource = _config.ResolveXlsxPath(),
                    ExportCsv = _exportCsv,
                    WinBuckets = new List<WinDistributionBucket>
                    {
                        new() { Label = "0", MinPayoutInclusive = 0, MaxPayoutInclusive = 0 },
                        new() { Label = "1-9", MinPayoutInclusive = 1, MaxPayoutInclusive = 9 },
                        new() { Label = "10-49", MinPayoutInclusive = 10, MaxPayoutInclusive = 49 },
                        new() { Label = "50+", MinPayoutInclusive = 50, MaxPayoutInclusive = null }
                    }
                };

                SlotSimulationRunner runner = new();
                SlotSimulationReport report = runner.Run(request);

                _reportSummary =
                    $"Spins: {report.SpinCount}\n" +
                    $"Seed: {report.Seed}\n" +
                    $"Source: {report.SelectedMathSource}\n" +
                    $"RTP: {report.RTP:F6}\n" +
                    $"Hit Frequency: {report.HitFrequency:P2}\n" +
                    $"Total Payout: {report.TotalPayout}\n" +
                    $"CSV: {(string.IsNullOrWhiteSpace(report.CsvPath) ? "not exported" : report.CsvPath)}";
            }
            catch (Exception ex)
            {
                _reportSummary = $"Simulation failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }
    }
}
