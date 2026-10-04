using System;
using Match3.Core;

namespace Match3.Tests
{
    /// <summary>
    /// An IRandom that returns exactly the numbers the test gives it, in order.
    /// It lets a test decide which colors the refill produces.
    /// It throws if Core asks for more numbers than the test scripted, or for a number that is out of range,
    /// so a surprise use of randomness fails loudly. Check Remaining == 0 at the end to prove every number was used.
    /// </summary>
    public sealed class ScriptedRandom : IRandom
    {
        private readonly int[] _values;
        private int _next;

        public ScriptedRandom(params int[] values)
        {
            _values = values;
        }

        public int Remaining => _values.Length - _next;

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_next >= _values.Length)
            {
                throw new InvalidOperationException("ScriptedRandom ran out of values: Core asked for more randomness than the test expected.");
            }

            int value = _values[_next++];
            if (value < minInclusive || value >= maxExclusive)
            {
                throw new InvalidOperationException(
                    "Scripted value " + value + " is outside [" + minInclusive + ", " + maxExclusive + ").");
            }

            return value;
        }
    }
}
