using System;

namespace Scripts.Core.Engine
{
    public interface IRNGProvider
    {
        int NextInt(int minInclusive, int maxExclusive);
    }

    public class SeededRNGProvider : IRNGProvider
    {
        private readonly Random _random;

        public SeededRNGProvider(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
