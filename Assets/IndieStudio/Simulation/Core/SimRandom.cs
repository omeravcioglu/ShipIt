using System;
using System.Collections.Generic;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Deterministic PCG32 random stream. The same seed produces the same sequence on every
    /// platform and runtime, which keeps balancing runs and bug reports reproducible.
    /// </summary>
    public sealed class SimRandom
    {
        ulong _state;
        readonly ulong _increment;

        public SimRandom(ulong seed, ulong stream)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        SimRandom(ulong state, ulong increment, bool restored)
        {
            _state = state;
            _increment = increment;
        }

        /// <summary>The generator's exact position, for save games: <see cref="Restore"/> continues the same sequence.</summary>
        internal ulong State => _state;
        internal ulong Increment => _increment;

        internal static SimRandom Restore(ulong state, ulong increment) => new SimRandom(state, increment, true);

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * 6364136223846793005UL + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => NextUInt() / 4294967296.0;

        public double Range(double min, double max) => min + (max - min) * NextDouble();

        /// <summary>Uniform integer in [min, max).</summary>
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextDouble() * (max - min));

        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>Standard normal sample (Box–Muller).</summary>
        public double Gaussian()
        {
            double u1 = 1.0 - NextDouble();
            double u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>Heavy-tailed sample whose median is <paramref name="median"/>.</summary>
        public double LogNormal(double median, double sigma) => median * Math.Exp(Gaussian() * sigma);

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        public T PickWeighted<T>(IReadOnlyList<T> items, Func<T, double> weight)
        {
            double total = 0;
            for (int i = 0; i < items.Count; i++) total += Math.Max(0, weight(items[i]));
            if (total <= 0) return Pick(items);
            double roll = NextDouble() * total;
            for (int i = 0; i < items.Count; i++)
            {
                roll -= Math.Max(0, weight(items[i]));
                if (roll < 0) return items[i];
            }
            return items[items.Count - 1];
        }
    }

    /// <summary>String hashing that is stable across processes (unlike string.GetHashCode).</summary>
    public static class StableHash
    {
        public static ulong Fnv1a64(string text)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in text)
            {
                hash ^= c;
                hash = unchecked(hash * 1099511628211UL);
            }
            return hash;
        }
    }
}
