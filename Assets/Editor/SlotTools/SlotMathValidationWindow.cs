using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts.Core.Math;
using Scripts.Core.MathLoading;
using UnityEditor;
using UnityEngine;

namespace Scripts.Editor.SlotTools
{
    public class SlotMathValidationWindow : EditorWindow
    {
        private SlotMathConfig _config;
        private Vector2 _scroll;
        private readonly List<string> _issues = new();
        private string _summary = "No validation run yet.";

        public static void Open(SlotMathConfig config)
        {
            SlotMathValidationWindow window = GetWindow<SlotMathValidationWindow>("Slot Math Validation");
            window._config = config;
            window.Show();
        }

        private void OnGUI()
        {
            _config = (SlotMathConfig)EditorGUILayout.ObjectField("Config", _config, typeof(SlotMathConfig), false);
            if (_config == null)
            {
                EditorGUILayout.HelpBox("Assign a SlotMathConfig asset.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Validate Math"))
            {
                RunValidation();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_summary, _issues.Count == 0 ? MessageType.Info : MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _issues.Count; i++)
            {
                EditorGUILayout.HelpBox(_issues[i], MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            _issues.Clear();

            string sourcePath = _config.ResolveXlsxPath();
            if (!File.Exists(sourcePath))
            {
                _issues.Add($"Workbook not found: {sourcePath}");
                _summary = "Validation failed.";
                return;
            }

            SlotMathModel model;
            try
            {
                model = SlotMathLoader.LoadFromXlsx(sourcePath);
            }
            catch (Exception ex)
            {
                _issues.Add($"Schema/load error: {ex.Message}");
                _summary = "Validation failed during schema parsing.";
                return;
            }

            ValidateReferentialIntegrity(model);

            _summary = _issues.Count == 0
                ? "Validation passed. No schema or referential issues found."
                : $"Validation completed with {_issues.Count} issue(s).";
        }

        private void ValidateReferentialIntegrity(SlotMathModel model)
        {
            HashSet<int> symbolIds = model.Symbols.Select(symbol => symbol.Id).ToHashSet();

            foreach (ReelStrip reel in model.Reels)
            {
                if (reel.OrderedSymbolIds.Count < model.Config.VisibleRows)
                {
                    _issues.Add($"Reel {reel.ReelIndex} length ({reel.OrderedSymbolIds.Count}) is shorter than VisibleRows ({model.Config.VisibleRows}).");
                }

                foreach (int symbolId in reel.OrderedSymbolIds)
                {
                    if (!symbolIds.Contains(symbolId))
                    {
                        _issues.Add($"Reel {reel.ReelIndex} references undefined symbol ID {symbolId}.");
                    }
                }
            }

            Dictionary<int, int> payoutRowsBySymbol = model.Paytable
                .GroupBy(entry => entry.SymbolId)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (SymbolData symbol in model.Symbols)
            {
                if (!payoutRowsBySymbol.ContainsKey(symbol.Id))
                {
                    _issues.Add($"Symbol '{symbol.Code}' (ID {symbol.Id}) has no paytable entries.");
                }
            }

            foreach (int reelIndex in model.Config.BonusEligibleReelIndices)
            {
                if (model.Reels.All(reel => reel.ReelIndex != reelIndex))
                {
                    _issues.Add($"Config references BonusEligibleReelIndex {reelIndex}, but that reel does not exist.");
                }
            }

            bool hasBonusSymbol = model.Symbols.Any(symbol => symbol.IsBonus);
            if (model.BonusPaytable.Count > 0 && !hasBonusSymbol)
            {
                _issues.Add("BonusPaytable is defined but no symbol is marked IsBonus.");
            }
        }
    }
}
