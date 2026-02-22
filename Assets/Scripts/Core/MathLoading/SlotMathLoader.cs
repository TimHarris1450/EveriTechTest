using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts.Core.Math;

namespace Scripts.Core.MathLoading
{
    public static class SlotMathLoader
    {
        private static readonly string[] RequiredSheets = { "Symbols", "ReelStrips", "Paytable" };

        public static SlotMathModel LoadFromXlsx(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new InvalidDataException("Slot math path is empty.");
            }

            if (!File.Exists(xlsxPath))
            {
                throw new FileNotFoundException($"Slot math workbook not found at '{xlsxPath}'.", xlsxPath);
            }

            Dictionary<string, List<Dictionary<string, string>>> workbook = XlsxSheetReader.ReadWorkbook(xlsxPath);

            foreach (string requiredSheet in RequiredSheets)
            {
                if (!workbook.ContainsKey(requiredSheet))
                {
                    throw new InvalidDataException($"Missing required sheet '{requiredSheet}' in '{xlsxPath}'.");
                }
            }

            List<SymbolData> symbols = ParseSymbols(workbook["Symbols"]);
            Dictionary<int, SymbolData> symbolById = symbols.ToDictionary(symbol => symbol.Id);
            List<ReelStrip> reels = ParseReels(workbook["ReelStrips"], symbolById);
            List<PaytableEntry> paytable = ParsePaytable(workbook["Paytable"], symbolById);
            List<BonusPaytableEntry> bonusPaytable = ParseBonusPaytable(
                workbook.TryGetValue("BonusPaytable", out List<Dictionary<string, string>> bonusPaytableRows)
                    ? bonusPaytableRows
                    : null);
            SlotMathRuntimeConfig config = ParseConfig(workbook.TryGetValue("Config", out List<Dictionary<string, string>> configRows) ? configRows : null, reels);

            return new SlotMathModel
            {
                Symbols = symbols,
                Reels = reels,
                Paytable = paytable,
                BonusPaytable = bonusPaytable,
                Config = config
            };
        }

        private static List<BonusPaytableEntry> ParseBonusPaytable(IReadOnlyList<Dictionary<string, string>> rows)
        {
            List<BonusPaytableEntry> table = new();
            if (rows == null || rows.Count == 0)
            {
                return table;
            }

            SheetParserHelpers.RequireColumns("BonusPaytable", rows, "Count", "Payout");
            HashSet<int> seenCounts = new();
            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                int rowNumber = i + 2;
                int count = SheetParserHelpers.GetRequiredInt("BonusPaytable", row, "Count", rowNumber);
                string payoutRaw = SheetParserHelpers.GetRequiredString("BonusPaytable", row, "Payout", rowNumber);
                if (!long.TryParse(payoutRaw, out long payout))
                {
                    throw new InvalidDataException($"BonusPaytable row {rowNumber} has invalid Payout '{payoutRaw}'.");
                }
                if (count < 1 || payout < 0)
                {
                    throw new InvalidDataException($"BonusPaytable row {rowNumber} is invalid. Count must be >= 1 and Payout must be >= 0.");
                }

                if (!seenCounts.Add(count))
                {
                    throw new InvalidDataException($"BonusPaytable row {rowNumber} duplicates Count '{count}'.");
                }

                table.Add(new BonusPaytableEntry
                {
                    Count = count,
                    Payout = payout
                });
            }

            return table;
        }

        private static List<SymbolData> ParseSymbols(IReadOnlyList<Dictionary<string, string>> rows)
        {
            SheetParserHelpers.RequireColumns("Symbols", rows, "SymbolId", "Code", "PrefabKey");

            List<SymbolData> symbols = new();
            HashSet<int> seenIds = new();

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                int rowNumber = i + 2;

                int id = SheetParserHelpers.GetRequiredInt("Symbols", row, "SymbolId", rowNumber);
                if (id < 0)
                {
                    throw new InvalidDataException($"Symbols row {rowNumber} has invalid SymbolId '{id}'. SymbolId must be >= 0.");
                }

                if (!seenIds.Add(id))
                {
                    throw new InvalidDataException($"Symbols row {rowNumber} duplicates SymbolId '{id}'.");
                }

                symbols.Add(new SymbolData
                {
                    Id = id,
                    Code = SheetParserHelpers.GetRequiredString("Symbols", row, "Code", rowNumber),
                    PrefabKey = SheetParserHelpers.GetRequiredString("Symbols", row, "PrefabKey", rowNumber),
                    IsWild = SheetParserHelpers.GetOptionalBool("Symbols", row, "IsWild", rowNumber),
                    IsScatter = SheetParserHelpers.GetOptionalBool("Symbols", row, "IsScatter", rowNumber),
                    IsBonus = SheetParserHelpers.GetOptionalBool("Symbols", row, "IsBonus", rowNumber)
                });
            }

            if (symbols.Count == 0)
            {
                throw new InvalidDataException("Symbols sheet has no data rows.");
            }

            return symbols;
        }

        private static List<ReelStrip> ParseReels(IReadOnlyList<Dictionary<string, string>> rows, IReadOnlyDictionary<int, SymbolData> symbols)
        {
            SheetParserHelpers.RequireColumns("ReelStrips", rows, "ReelIndex", "Order", "SymbolId");

            Dictionary<int, List<(int order, int symbolId, int rowNumber)>> temp = new();

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                int rowNumber = i + 2;

                int reelIndex = SheetParserHelpers.GetRequiredInt("ReelStrips", row, "ReelIndex", rowNumber);
                int order = SheetParserHelpers.GetRequiredInt("ReelStrips", row, "Order", rowNumber);
                int symbolId = SheetParserHelpers.GetRequiredInt("ReelStrips", row, "SymbolId", rowNumber);

                if (reelIndex < 0)
                {
                    throw new InvalidDataException($"ReelStrips row {rowNumber} has invalid ReelIndex '{reelIndex}'. ReelIndex must be >= 0.");
                }

                if (order < 0)
                {
                    throw new InvalidDataException($"ReelStrips row {rowNumber} has invalid Order '{order}'. Order must be >= 0.");
                }

                if (!symbols.ContainsKey(symbolId))
                {
                    throw new InvalidDataException($"ReelStrips row {rowNumber} references unknown SymbolId '{symbolId}'.");
                }

                if (!temp.TryGetValue(reelIndex, out List<(int order, int symbolId, int rowNumber)> strip))
                {
                    strip = new List<(int order, int symbolId, int rowNumber)>();
                    temp[reelIndex] = strip;
                }

                if (strip.Any(node => node.order == order))
                {
                    throw new InvalidDataException($"ReelStrips row {rowNumber} duplicates Order '{order}' for ReelIndex '{reelIndex}'.");
                }

                strip.Add((order, symbolId, rowNumber));
            }

            List<ReelStrip> reels = temp
                .OrderBy(pair => pair.Key)
                .Select(pair => new ReelStrip
                {
                    ReelIndex = pair.Key,
                    OrderedSymbolIds = pair.Value
                        .OrderBy(node => node.order)
                        .Select(node => node.symbolId)
                        .ToList()
                })
                .ToList();

            if (reels.Count == 0)
            {
                throw new InvalidDataException("ReelStrips sheet has no data rows.");
            }

            foreach (ReelStrip reel in reels)
            {
                if (reel.OrderedSymbolIds.Count == 0)
                {
                    throw new InvalidDataException($"Reel {reel.ReelIndex} has an empty strip.");
                }
            }

            return reels;
        }

        private static List<PaytableEntry> ParsePaytable(IReadOnlyList<Dictionary<string, string>> rows, IReadOnlyDictionary<int, SymbolData> symbols)
        {
            SheetParserHelpers.RequireColumns("Paytable", rows, "SymbolId", "Count", "Payout");

            List<PaytableEntry> paytable = new();
            HashSet<(int symbolId, int count)> seenRows = new();

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                int rowNumber = i + 2;
                int symbolId = SheetParserHelpers.GetRequiredInt("Paytable", row, "SymbolId", rowNumber);
                int matchCount = SheetParserHelpers.GetRequiredInt("Paytable", row, "Count", rowNumber);
                string payoutRaw = SheetParserHelpers.GetRequiredString("Paytable", row, "Payout", rowNumber);
                if (!long.TryParse(payoutRaw, out long payout))
                {
                    throw new InvalidDataException($"Paytable row {rowNumber} has invalid Payout '{payoutRaw}'.");
                }

                if (!symbols.ContainsKey(symbolId))
                {
                    throw new InvalidDataException($"Paytable row {rowNumber} references unknown SymbolId '{symbolId}'.");
                }

                if (matchCount < 1 || payout < 0)
                {
                    throw new InvalidDataException($"Paytable row {rowNumber} is invalid. Count must be >= 1 and Payout must be >= 0.");
                }

                if (!seenRows.Add((symbolId, matchCount)))
                {
                    throw new InvalidDataException($"Paytable row {rowNumber} duplicates SymbolId '{symbolId}' with Count '{matchCount}'.");
                }

                paytable.Add(new PaytableEntry
                {
                    SymbolId = symbolId,
                    MatchCount = matchCount,
                    Payout = payout
                });
            }

            if (paytable.Count == 0)
            {
                throw new InvalidDataException("Paytable sheet has no data rows.");
            }

            return paytable;
        }

        private static SlotMathRuntimeConfig ParseConfig(IReadOnlyList<Dictionary<string, string>> rows, IReadOnlyList<ReelStrip> reels)
        {
            SlotMathRuntimeConfig config = new();
            if (rows == null || rows.Count == 0)
            {
                return config;
            }

            SheetParserHelpers.RequireColumns("Config", rows, "Key", "Value");

            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                int rowNumber = i + 2;
                string key = SheetParserHelpers.GetRequiredString("Config", row, "Key", rowNumber);
                string value = SheetParserHelpers.GetRequiredString("Config", row, "Value", rowNumber);
                values[key] = value;
            }

            if (values.TryGetValue("VisibleRows", out string visibleRowsRaw))
            {
                if (!int.TryParse(visibleRowsRaw, out int visibleRows) || visibleRows < 1)
                {
                    throw new InvalidDataException("Config key 'VisibleRows' must be an integer greater than 0.");
                }

                config.VisibleRows = visibleRows;
            }

            if (values.TryGetValue("BonusEligibleReelIndices", out string bonusReelsRaw))
            {
                List<int> parsed = new();
                foreach (string token in bonusReelsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!int.TryParse(token.Trim(), out int reelIndex))
                    {
                        throw new InvalidDataException($"Config key 'BonusEligibleReelIndices' has invalid reel index '{token}'.");
                    }

                    if (reelIndex < 0)
                    {
                        throw new InvalidDataException($"Config key 'BonusEligibleReelIndices' has negative reel index '{reelIndex}'.");
                    }

                    if (parsed.Contains(reelIndex))
                    {
                        throw new InvalidDataException($"Config key 'BonusEligibleReelIndices' contains duplicate reel index '{reelIndex}'.");
                    }

                    parsed.Add(reelIndex);
                }

                config.BonusEligibleReelIndices = parsed;
            }

            if (values.TryGetValue("ReelCount", out string reelCountRaw) && int.TryParse(reelCountRaw, out int reelCount))
            {
                if (reelCount != reels.Count)
                {
                    throw new InvalidDataException($"Config key 'ReelCount' is {reelCount}, but ReelStrips contains {reels.Count} reels.");
                }
            }

            if (values.TryGetValue("PayoutMode", out string payoutModeRaw))
            {
                if (!Enum.TryParse(payoutModeRaw, true, out PayoutMode payoutMode))
                {
                    throw new InvalidDataException($"Config key 'PayoutMode' has invalid value '{payoutModeRaw}'.");
                }

                config.PayoutMode = payoutMode;
            }

            return config;
        }
    }
}
