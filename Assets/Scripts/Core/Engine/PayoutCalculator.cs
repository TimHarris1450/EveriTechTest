using System.Collections.Generic;
using Scripts.Core.Math;

namespace Scripts.Core.Engine
{
    public class PayoutCalculator
    {
        private readonly SlotMathModel _model;
        private readonly List<SymbolData> _scatterSymbols = new();

        public PayoutCalculator(SlotMathModel model)
        {
            _model = model;

            for (int i = 0; i < _model.Symbols.Count; i++)
            {
                SymbolData symbol = _model.Symbols[i];
                if (symbol.IsScatter || symbol.IsBonus)
                {
                    _scatterSymbols.Add(symbol);
                }
            }
        }

        public void Evaluate(SpinResult result, bool includeDetails = true)
        {
            long totalPayout = 0;
            totalPayout += EvaluateConfiguredPrimaryMode(result, includeDetails);
            totalPayout += EvaluateScatter(result, includeDetails);
            totalPayout += EvaluateBonusPaytable(result, includeDetails);
            result.TotalPayout = totalPayout;

            if (includeDetails)
            {
                EvaluateFeatures(result);
            }
        }

        private long EvaluatePayline(SpinResult result, bool includeDetails)
        {
            if (result.LandedSymbolMatrix.Count == 0 || _model.Config.VisibleRows == 0)
            {
                return 0;
            }

            int middleRow = _model.Config.VisibleRows / 2;
            int firstSymbol = result.LandedSymbolMatrix[0][middleRow];
            int matchCount = 1;

            for (int i = 1; i < result.LandedSymbolMatrix.Count; i++)
            {
                if (result.LandedSymbolMatrix[i][middleRow] != firstSymbol)
                {
                    break;
                }

                matchCount++;
            }

            PaytableEntry paylineEntry = FindBestPaytableEntry(firstSymbol, matchCount);
            if (paylineEntry == null)
            {
                return 0;
            }

            if (includeDetails)
            {
                result.LineWins.Add(new LineWin
                {
                    Type = "payline",
                    SymbolId = firstSymbol,
                    MatchCount = paylineEntry.MatchCount,
                    Ways = 1,
                    Payout = paylineEntry.Payout
                });
            }

            return paylineEntry.Payout;
        }

        private long EvaluateConfiguredPrimaryMode(SpinResult result, bool includeDetails)
        {
            switch (_model.Config.PayoutMode)
            {
                case PayoutMode.SingleCenterLine:
                default:
                    return EvaluatePayline(result, includeDetails);
            }
        }

        private long EvaluateScatter(SpinResult result, bool includeDetails)
        {
            long totalPayout = 0;

            for (int symbolIndex = 0; symbolIndex < _scatterSymbols.Count; symbolIndex++)
            {
                SymbolData symbol = _scatterSymbols[symbolIndex];
                int totalCount = 0;

                for (int reelIndex = 0; reelIndex < result.LandedSymbolMatrix.Count; reelIndex++)
                {
                    List<int> reel = result.LandedSymbolMatrix[reelIndex];
                    for (int rowIndex = 0; rowIndex < reel.Count; rowIndex++)
                    {
                        if (reel[rowIndex] == symbol.Id)
                        {
                            totalCount++;
                        }
                    }
                }

                PaytableEntry scatterEntry = FindBestPaytableEntry(symbol.Id, totalCount);
                if (scatterEntry == null)
                {
                    continue;
                }

                totalPayout += scatterEntry.Payout;
                if (includeDetails)
                {
                    result.ScatterWins.Add(new ScatterWin
                    {
                        SymbolId = symbol.Id,
                        Count = totalCount,
                        Payout = scatterEntry.Payout
                    });
                }
            }

            return totalPayout;
        }

        private long EvaluateBonusPaytable(SpinResult result, bool includeDetails)
        {
            if (_model.BonusPaytable == null || _model.BonusPaytable.Count == 0)
            {
                return 0;
            }

            SymbolData bonusSymbol = null;
            for (int i = 0; i < _model.Symbols.Count; i++)
            {
                if (_model.Symbols[i].IsBonus)
                {
                    bonusSymbol = _model.Symbols[i];
                    break;
                }
            }

            if (bonusSymbol == null)
            {
                return 0;
            }

            int totalCount = 0;
            for (int reelIndex = 0; reelIndex < result.LandedSymbolMatrix.Count; reelIndex++)
            {
                List<int> reel = result.LandedSymbolMatrix[reelIndex];
                for (int rowIndex = 0; rowIndex < reel.Count; rowIndex++)
                {
                    if (reel[rowIndex] == bonusSymbol.Id)
                    {
                        totalCount++;
                    }
                }
            }

            BonusPaytableEntry bestEntry = null;
            for (int i = 0; i < _model.BonusPaytable.Count; i++)
            {
                BonusPaytableEntry entry = _model.BonusPaytable[i];
                if (entry.Count > totalCount)
                {
                    continue;
                }

                if (bestEntry == null || entry.Count > bestEntry.Count)
                {
                    bestEntry = entry;
                }
            }

            if (bestEntry == null)
            {
                return 0;
            }

            if (includeDetails)
            {
                result.LineWins.Add(new LineWin
                {
                    Type = "bonus_paytable",
                    SymbolId = bonusSymbol.Id,
                    MatchCount = bestEntry.Count,
                    Ways = 1,
                    Payout = bestEntry.Payout
                });
            }

            return bestEntry.Payout;
        }

        private PaytableEntry FindBestPaytableEntry(int symbolId, int matchedCount)
        {
            PaytableEntry bestEntry = null;
            for (int i = 0; i < _model.Paytable.Count; i++)
            {
                PaytableEntry entry = _model.Paytable[i];
                if (entry.SymbolId != symbolId || entry.MatchCount > matchedCount)
                {
                    continue;
                }

                if (bestEntry == null || entry.MatchCount > bestEntry.MatchCount)
                {
                    bestEntry = entry;
                }
            }

            return bestEntry;
        }

        private void EvaluateFeatures(SpinResult result)
        {
            SymbolData bonusSymbol = null;
            for (int i = 0; i < _model.Symbols.Count; i++)
            {
                if (_model.Symbols[i].IsBonus)
                {
                    bonusSymbol = _model.Symbols[i];
                    break;
                }
            }

            if (bonusSymbol == null)
            {
                return;
            }

            int bonusCount = 0;
            for (int reelIndex = 0; reelIndex < result.LandedSymbolMatrix.Count; reelIndex++)
            {
                List<int> reel = result.LandedSymbolMatrix[reelIndex];
                for (int rowIndex = 0; rowIndex < reel.Count; rowIndex++)
                {
                    if (reel[rowIndex] == bonusSymbol.Id)
                    {
                        bonusCount++;
                    }
                }
            }

            if (bonusCount >= 2)
            {
                result.TriggeredFeatures.Add("bonus_anticipation");
            }

            if (bonusCount >= 3)
            {
                result.TriggeredFeatures.Add("bonus_triggered");
            }
        }
    }
}
