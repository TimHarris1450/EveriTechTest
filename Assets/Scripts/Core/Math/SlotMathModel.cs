using System;
using System.Collections.Generic;

namespace Scripts.Core.Math
{
    [Serializable]
    public class SymbolData
    {
        public int Id;
        public string Code;
        public string PrefabKey;
        public bool IsWild;
        public bool IsScatter;
        public bool IsBonus;
    }

    [Serializable]
    public class ReelStrip
    {
        public int ReelIndex;
        public List<int> OrderedSymbolIds = new();
    }

    [Serializable]
    public class PaytableEntry
    {
        public int SymbolId;
        public int MatchCount;
        public int Payout;
    }

    [Serializable]
    public class SlotMathConfig
    {
        public int VisibleRows = 3;
        public List<int> BonusEligibleReelIndices = new();
    }

    [Serializable]
    public class SlotMathModel
    {
        public List<SymbolData> Symbols = new();
        public List<ReelStrip> Reels = new();
        public List<PaytableEntry> Paytable = new();
        public SlotMathConfig Config = new();
    }
}
