using System;
using System.Collections.Generic;

namespace Scripts.Core.Engine
{
    [Serializable]
    public class LineWin
    {
        public string Type;
        public int SymbolId;
        public int MatchCount;
        public int Ways;
        public int Payout;
    }

    [Serializable]
    public class ScatterWin
    {
        public int SymbolId;
        public int Count;
        public int Payout;
    }

    [Serializable]
    public class SpinResult
    {
        public List<int> ReelStopIndices = new();
        public List<List<int>> LandedSymbolMatrix = new();
        public List<LineWin> LineWins = new();
        public List<ScatterWin> ScatterWins = new();
        public int TotalPayout;
        public List<string> TriggeredFeatures = new();

        public SpinResult()
        {
        }

        public SpinResult(int reelCount, int visibleRows)
        {
            EnsureBufferCapacity(reelCount, visibleRows);
        }

        public void EnsureBufferCapacity(int reelCount, int visibleRows)
        {
            ReelStopIndices.Clear();
            for (int i = 0; i < reelCount; i++)
            {
                ReelStopIndices.Add(0);
            }

            if (LandedSymbolMatrix.Count > reelCount)
            {
                LandedSymbolMatrix.RemoveRange(reelCount, LandedSymbolMatrix.Count - reelCount);
            }

            for (int reelIndex = 0; reelIndex < reelCount; reelIndex++)
            {
                if (reelIndex >= LandedSymbolMatrix.Count)
                {
                    LandedSymbolMatrix.Add(new List<int>(visibleRows));
                }

                List<int> reelSymbols = LandedSymbolMatrix[reelIndex];
                reelSymbols.Clear();
                for (int rowIndex = 0; rowIndex < visibleRows; rowIndex++)
                {
                    reelSymbols.Add(0);
                }
            }

            ResetForReuse();
        }

        public void ResetForReuse()
        {
            LineWins.Clear();
            ScatterWins.Clear();
            TriggeredFeatures.Clear();
            TotalPayout = 0;
        }
    }
}
