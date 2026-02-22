using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Core.Math
{
    public class SlotMathEngine
    {
        private readonly SlotMathModel _model;
        private readonly Random _random;

        public SlotMathEngine(SlotMathModel model, int? seed = null)
        {
            _model = model;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public SlotMathModel Model => _model;

        public IReadOnlyList<int> ResolveStopSymbolsForReel(int reelIndex)
        {
            ReelStrip reel = _model.Reels.First(r => r.ReelIndex == reelIndex);
            int startIndex = _random.Next(0, reel.OrderedSymbolIds.Count);
            List<int> stopSymbols = new();

            for (int row = 0; row < _model.Config.VisibleRows; row++)
            {
                int stripIndex = (startIndex + row) % reel.OrderedSymbolIds.Count;
                stopSymbols.Add(reel.OrderedSymbolIds[stripIndex]);
            }

            return stopSymbols;
        }
    }
}
