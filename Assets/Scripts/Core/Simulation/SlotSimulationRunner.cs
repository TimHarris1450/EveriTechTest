using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Scripts.Core.Engine;
using Scripts.Core.Math;

namespace Scripts.Core.Simulation
{
    [Serializable]
    public class WinDistributionBucket
    {
        public string Label;
        public int MinPayoutInclusive;
        public int? MaxPayoutInclusive;
        public long SpinCount;
    }

    [Serializable]
    public class SymbolLandingFrequencyRow
    {
        public int ReelIndex;
        public int RowIndex;
        public Dictionary<int, long> SymbolHits = new();
    }

    [Serializable]
    public class SlotSimulationRequest
    {
        public int SpinCount;
        public int Seed;
        public SlotMathModel MathModel;
        public string SelectedMathSource;
        public List<WinDistributionBucket> WinBuckets = new();
        public bool ExportCsv;
        public string ReportDirectory = Path.Combine("Assets", "StreamingAssets", "SimulationReports");
        public string ReportName = "slot-simulation-report";
    }

    [Serializable]
    public class SlotSimulationReport
    {
        public int SpinCount;
        public int Seed;
        public string SelectedMathSource;
        public long TotalPayout;
        public double RTP;
        public long HitCount;
        public double HitFrequency;
        public List<WinDistributionBucket> WinDistribution = new();
        public List<SymbolLandingFrequencyRow> SymbolLandingFrequency = new();
        public string CsvPath;
    }

    public class SlotSimulationRunner
    {
        public SlotSimulationReport Run(SlotSimulationRequest request)
        {
            ValidateRequest(request);

            int reelCount = request.MathModel.Reels.Count;
            int rowCount = request.MathModel.Config.VisibleRows;
            var rng = new SeededRNGProvider(request.Seed);
            var slotEngine = new SlotEngine(request.MathModel, rng);
            var spinResultBuffer = new SpinResult(reelCount, rowCount);

            var symbolIds = new List<int>(request.MathModel.Symbols.Count);
            var symbolIdToIndex = new Dictionary<int, int>(request.MathModel.Symbols.Count);
            for (int i = 0; i < request.MathModel.Symbols.Count; i++)
            {
                int symbolId = request.MathModel.Symbols[i].Id;
                symbolIdToIndex[symbolId] = i;
                symbolIds.Add(symbolId);
            }

            var bucketCounters = new long[request.WinBuckets.Count];
            var landingCounters = new long[reelCount, rowCount, symbolIds.Count];

            long totalPayout = 0;
            long hitCount = 0;

            for (int spinIndex = 0; spinIndex < request.SpinCount; spinIndex++)
            {
                SpinResult spinResult = slotEngine.Spin(spinResultBuffer, includeDetailedWins: false);
                int spinPayout = spinResult.TotalPayout;

                totalPayout += spinPayout;
                if (spinPayout > 0)
                {
                    hitCount++;
                }

                for (int bucketIndex = 0; bucketIndex < request.WinBuckets.Count; bucketIndex++)
                {
                    WinDistributionBucket bucket = request.WinBuckets[bucketIndex];
                    bool inRange = spinPayout >= bucket.MinPayoutInclusive &&
                                   (!bucket.MaxPayoutInclusive.HasValue || spinPayout <= bucket.MaxPayoutInclusive.Value);
                    if (inRange)
                    {
                        bucketCounters[bucketIndex]++;
                        break;
                    }
                }

                for (int reelIndex = 0; reelIndex < reelCount; reelIndex++)
                {
                    List<int> reelSymbols = spinResult.LandedSymbolMatrix[reelIndex];
                    for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                    {
                        int symbolId = reelSymbols[rowIndex];
                        int symbolIndex = symbolIdToIndex[symbolId];
                        landingCounters[reelIndex, rowIndex, symbolIndex]++;
                    }
                }
            }

            SlotSimulationReport report = BuildReport(request, symbolIds, totalPayout, hitCount, bucketCounters, landingCounters);

            if (request.ExportCsv)
            {
                report.CsvPath = ExportCsv(request, report);
            }

            return report;
        }

        private static SlotSimulationReport BuildReport(
            SlotSimulationRequest request,
            List<int> symbolIds,
            long totalPayout,
            long hitCount,
            long[] bucketCounters,
            long[,,] landingCounters)
        {
            SlotSimulationReport report = new()
            {
                SpinCount = request.SpinCount,
                Seed = request.Seed,
                SelectedMathSource = request.SelectedMathSource,
                TotalPayout = totalPayout,
                RTP = request.SpinCount == 0 ? 0d : (double)totalPayout / request.SpinCount,
                HitCount = hitCount,
                HitFrequency = request.SpinCount == 0 ? 0d : (double)hitCount / request.SpinCount
            };

            for (int bucketIndex = 0; bucketIndex < request.WinBuckets.Count; bucketIndex++)
            {
                WinDistributionBucket inputBucket = request.WinBuckets[bucketIndex];
                report.WinDistribution.Add(new WinDistributionBucket
                {
                    Label = inputBucket.Label,
                    MinPayoutInclusive = inputBucket.MinPayoutInclusive,
                    MaxPayoutInclusive = inputBucket.MaxPayoutInclusive,
                    SpinCount = bucketCounters[bucketIndex]
                });
            }

            int reelCount = landingCounters.GetLength(0);
            int rowCount = landingCounters.GetLength(1);
            for (int reelIndex = 0; reelIndex < reelCount; reelIndex++)
            {
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    SymbolLandingFrequencyRow row = new()
                    {
                        ReelIndex = reelIndex,
                        RowIndex = rowIndex
                    };

                    for (int symbolIndex = 0; symbolIndex < symbolIds.Count; symbolIndex++)
                    {
                        row.SymbolHits[symbolIds[symbolIndex]] = landingCounters[reelIndex, rowIndex, symbolIndex];
                    }

                    report.SymbolLandingFrequency.Add(row);
                }
            }

            return report;
        }

        private static string ExportCsv(SlotSimulationRequest request, SlotSimulationReport report)
        {
            Directory.CreateDirectory(request.ReportDirectory);
            string safeReportName = string.IsNullOrWhiteSpace(request.ReportName)
                ? "slot-simulation-report"
                : request.ReportName.Trim();
            string fileName = $"{safeReportName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            string outputPath = Path.Combine(request.ReportDirectory, fileName);

            StringBuilder builder = new();
            builder.AppendLine("metric,value");
            builder.AppendLine($"spin_count,{report.SpinCount}");
            builder.AppendLine($"seed,{report.Seed}");
            builder.AppendLine($"selected_math_source,{EscapeCsv(report.SelectedMathSource)}");
            builder.AppendLine($"total_payout,{report.TotalPayout}");
            builder.AppendLine($"rtp,{report.RTP.ToString(CultureInfo.InvariantCulture)}");
            builder.AppendLine($"hit_count,{report.HitCount}");
            builder.AppendLine($"hit_frequency,{report.HitFrequency.ToString(CultureInfo.InvariantCulture)}");
            builder.AppendLine();

            builder.AppendLine("bucket_label,min_payout,max_payout,spins");
            for (int i = 0; i < report.WinDistribution.Count; i++)
            {
                WinDistributionBucket bucket = report.WinDistribution[i];
                string maxPayout = bucket.MaxPayoutInclusive.HasValue
                    ? bucket.MaxPayoutInclusive.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                builder.AppendLine($"{EscapeCsv(bucket.Label)},{bucket.MinPayoutInclusive},{maxPayout},{bucket.SpinCount}");
            }

            builder.AppendLine();
            builder.AppendLine("reel,row,symbol_id,hits");
            for (int i = 0; i < report.SymbolLandingFrequency.Count; i++)
            {
                SymbolLandingFrequencyRow row = report.SymbolLandingFrequency[i];
                foreach (KeyValuePair<int, long> symbolHit in row.SymbolHits)
                {
                    builder.AppendLine($"{row.ReelIndex},{row.RowIndex},{symbolHit.Key},{symbolHit.Value}");
                }
            }

            File.WriteAllText(outputPath, builder.ToString());
            return outputPath;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static void ValidateRequest(SlotSimulationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.MathModel == null)
            {
                throw new ArgumentException("MathModel is required for simulation.", nameof(request));
            }

            if (request.SpinCount < 0)
            {
                throw new ArgumentException("SpinCount cannot be negative.", nameof(request));
            }

            if (request.WinBuckets == null)
            {
                request.WinBuckets = new List<WinDistributionBucket>();
            }

            if (string.IsNullOrWhiteSpace(request.SelectedMathSource))
            {
                request.SelectedMathSource = "unspecified";
            }
        }
    }
}
