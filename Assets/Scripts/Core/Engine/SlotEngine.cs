using Scripts.Core.Math;

namespace Scripts.Core.Engine
{
    public class SlotEngine
    {
        private readonly SpinResolver _spinResolver;
        private readonly PayoutCalculator _payoutCalculator;

        public SlotEngine(SlotMathModel model, IRNGProvider rngProvider)
        {
            _spinResolver = new SpinResolver(model, rngProvider);
            _payoutCalculator = new PayoutCalculator(model);
        }

        public SpinResult Spin()
        {
            SpinResult result = _spinResolver.Resolve();
            _payoutCalculator.Evaluate(result);
            return result;
        }

        public SpinResult Spin(SpinResult reusableResult, bool includeDetailedWins)
        {
            SpinResult result = _spinResolver.Resolve(reusableResult);
            _payoutCalculator.Evaluate(result, includeDetailedWins);
            return result;
        }
    }
}
