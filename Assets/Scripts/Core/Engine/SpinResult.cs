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
    }
}
