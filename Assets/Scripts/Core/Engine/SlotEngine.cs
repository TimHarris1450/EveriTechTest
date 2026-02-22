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
    }
}
