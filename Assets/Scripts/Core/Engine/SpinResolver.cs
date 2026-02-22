using System.Collections.Generic;
using System.Linq;
using Scripts.Core.Math;

namespace Scripts.Core.Engine
{
    public class SpinResolver
    {
        private readonly SlotMathModel _model;
        private readonly IRNGProvider _rngProvider;

        public SpinResolver(SlotMathModel model, IRNGProvider rngProvider)
        {
            _model = model;
            _rngProvider = rngProvider;
        }

        public SpinResult Resolve()
        {
            SpinResult result = new();

            foreach (ReelStrip reel in _model.Reels.OrderBy(r => r.ReelIndex))
            {
                int stopIndex = _rngProvider.NextInt(0, reel.OrderedSymbolIds.Count);
                result.ReelStopIndices.Add(stopIndex);

                List<int> reelSymbols = new();
                for (int row = 0; row < _model.Config.VisibleRows; row++)
                {
                    int stripIndex = (stopIndex + row) % reel.OrderedSymbolIds.Count;
                    reelSymbols.Add(reel.OrderedSymbolIds[stripIndex]);
                }

                result.LandedSymbolMatrix.Add(reelSymbols);
            }

            return result;
        }
    }
}
