using System.Collections.Generic;
using System.Linq;
using Scripts.Core.Math;

namespace Scripts.Core.Engine
{
    public class PayoutCalculator
    {
        private readonly SlotMathModel _model;

        public PayoutCalculator(SlotMathModel model)
        {
            _model = model;
        }

        public void Evaluate(SpinResult result)
        {
            EvaluatePayline(result);
            EvaluateWays(result);
            EvaluateScatter(result);
            result.TotalPayout = result.LineWins.Sum(win => win.Payout) + result.ScatterWins.Sum(win => win.Payout);
            EvaluateFeatures(result);
        }

        private void EvaluatePayline(SpinResult result)
        {
            if (result.LandedSymbolMatrix.Count == 0 || _model.Config.VisibleRows == 0)
            {
                return;
            }

            int middleRow = _model.Config.VisibleRows / 2;
            List<int> lineSymbols = result.LandedSymbolMatrix.Select(reel => reel[middleRow]).ToList();
            int firstSymbol = lineSymbols[0];
            int matchCount = 1;

            for (int i = 1; i < lineSymbols.Count; i++)
            {
                if (lineSymbols[i] != firstSymbol)
                {
                    break;
                }

                matchCount++;
            }

            PaytableEntry paylineEntry = _model.Paytable
                .Where(entry => entry.SymbolId == firstSymbol && entry.MatchCount <= matchCount)
                .OrderByDescending(entry => entry.MatchCount)
                .FirstOrDefault();

            if (paylineEntry != null)
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
        }

        private void EvaluateWays(SpinResult result)
        {
            foreach (SymbolData symbol in _model.Symbols.Where(s => !s.IsScatter && !s.IsBonus))
            {
                int ways = 1;
                int matchedReels = 0;

                foreach (List<int> reel in result.LandedSymbolMatrix)
                {
                    int reelMatches = reel.Count(symbolId => symbolId == symbol.Id);
                    if (reelMatches == 0)
                    {
                        break;
                    }

                    ways *= reelMatches;
                    matchedReels++;
                }

                PaytableEntry wayEntry = _model.Paytable
                    .Where(entry => entry.SymbolId == symbol.Id && entry.MatchCount <= matchedReels)
                    .OrderByDescending(entry => entry.MatchCount)
                    .FirstOrDefault();

                if (wayEntry != null)
                {
                    result.LineWins.Add(new LineWin
                    {
                        Type = "ways",
                        SymbolId = symbol.Id,
                        MatchCount = wayEntry.MatchCount,
                        Ways = ways,
                        Payout = wayEntry.Payout * ways
                    });
                }
            }
        }

        private void EvaluateScatter(SpinResult result)
        {
            foreach (SymbolData symbol in _model.Symbols.Where(s => s.IsScatter || s.IsBonus))
            {
                int totalCount = result.LandedSymbolMatrix.Sum(reel => reel.Count(symbolId => symbolId == symbol.Id));
                PaytableEntry scatterEntry = _model.Paytable
                    .Where(entry => entry.SymbolId == symbol.Id && entry.MatchCount <= totalCount)
                    .OrderByDescending(entry => entry.MatchCount)
                    .FirstOrDefault();

                if (scatterEntry != null)
                {
                    result.ScatterWins.Add(new ScatterWin
                    {
                        SymbolId = symbol.Id,
                        Count = totalCount,
                        Payout = scatterEntry.Payout
                    });
                }
            }
        }

        private void EvaluateFeatures(SpinResult result)
        {
            SymbolData bonusSymbol = _model.Symbols.FirstOrDefault(symbol => symbol.IsBonus);
            if (bonusSymbol == null)
            {
                return;
            }

            int bonusCount = result.LandedSymbolMatrix.Sum(reel => reel.Count(symbolId => symbolId == bonusSymbol.Id));
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
