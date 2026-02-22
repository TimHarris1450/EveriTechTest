using System.Collections.Generic;

namespace Scripts.Core.Math
{
    public static class DefaultSlotMathModel
    {
        public static SlotMathModel Create()
        {
            return new SlotMathModel
            {
                Symbols = new List<SymbolData>
                {
                    new() { Id = 0, Code = "BONUS", PrefabKey = "Bonus", IsBonus = true },
                    new() { Id = 1, Code = "9", PrefabKey = "9" },
                    new() { Id = 2, Code = "10", PrefabKey = "10" },
                    new() { Id = 3, Code = "J", PrefabKey = "J" },
                    new() { Id = 4, Code = "Q", PrefabKey = "Q" },
                    new() { Id = 5, Code = "K", PrefabKey = "K" },
                    new() { Id = 6, Code = "A", PrefabKey = "A" }
                },
                Reels = new List<ReelStrip>
                {
                    new() { ReelIndex = 0, OrderedSymbolIds = new List<int> { 1, 2, 3, 4, 5, 6, 1, 3, 5, 2 } },
                    new() { ReelIndex = 1, OrderedSymbolIds = new List<int> { 2, 4, 6, 3, 5, 1, 2, 6, 4, 3 } },
                    new() { ReelIndex = 2, OrderedSymbolIds = new List<int> { 3, 5, 1, 6, 4, 2, 3, 1, 5, 6 } },
                    new() { ReelIndex = 3, OrderedSymbolIds = new List<int> { 4, 6, 2, 5, 3, 1, 4, 2, 6, 5 } },
                    new() { ReelIndex = 4, OrderedSymbolIds = new List<int> { 5, 1, 3, 2, 6, 4, 5, 3, 1, 2 } }
                },
                Paytable = new List<PaytableEntry>
                {
                    new() { SymbolId = 6, MatchCount = 3, Payout = 20 },
                    new() { SymbolId = 6, MatchCount = 4, Payout = 40 },
                    new() { SymbolId = 6, MatchCount = 5, Payout = 80 }
                },
                Config = new SlotMathRuntimeConfig
                {
                    VisibleRows = 3,
                    BonusEligibleReelIndices = new List<int> { 1, 2, 3 }
                }
            };
        }
    }
}
