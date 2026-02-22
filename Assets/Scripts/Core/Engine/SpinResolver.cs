using System.Collections.Generic;
using Scripts.Core.Math;

namespace Scripts.Core.Engine
{
    public class SpinResolver
    {
        private readonly SlotMathModel _model;
        private readonly IRNGProvider _rngProvider;
        private readonly List<ReelStrip> _orderedReels;

        public SpinResolver(SlotMathModel model, IRNGProvider rngProvider)
        {
            _model = model;
            _rngProvider = rngProvider;
            _orderedReels = new List<ReelStrip>(_model.Reels);
            _orderedReels.Sort((left, right) => left.ReelIndex.CompareTo(right.ReelIndex));
        }

        public SpinResult Resolve()
        {
            SpinResult result = new(_orderedReels.Count, _model.Config.VisibleRows);
            return Resolve(result);
        }

        public SpinResult Resolve(SpinResult result)
        {
            int visibleRows = _model.Config.VisibleRows;
            result.EnsureBufferCapacity(_orderedReels.Count, visibleRows);
            result.ResetForReuse();

            for (int reelIndex = 0; reelIndex < _orderedReels.Count; reelIndex++)
            {
                ReelStrip reel = _orderedReels[reelIndex];
                int stopIndex = _rngProvider.NextInt(0, reel.OrderedSymbolIds.Count);
                result.ReelStopIndices[reelIndex] = stopIndex;

                List<int> reelSymbols = result.LandedSymbolMatrix[reelIndex];
                for (int row = 0; row < visibleRows; row++)
                {
                    int stripIndex = (stopIndex + row) % reel.OrderedSymbolIds.Count;
                    reelSymbols[row] = reel.OrderedSymbolIds[stripIndex];
                }
            }

            return result;
        }
    }
}
