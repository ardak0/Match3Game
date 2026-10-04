using DG.Tweening;

namespace Match3.Infrastructure
{
    /// <summary>
    /// One-time DOTween configuration for a game that plays many short tweens (a cascade is dozens of them).
    ///
    /// Recycling: when a tween finishes, DOTween keeps the object in a pool and reuses it for the next tween
    /// instead of leaving it for the garbage collector. This is the same idea as our tile pool.
    /// Capacity: DOTween pre-creates room for this many tweens, so it doesn't grow while playing.
    /// </summary>
    public static class DOTweenSetup
    {
        // A cascade wave on a 10x12 board can have well under 400 tweens alive at once.
        private const int TweenerCapacity = 400;
        private const int SequenceCapacity = 50;

        private static bool _configured;

        /// <summary>Safe to call many times; it only does the work the first time.</summary>
        public static void Configure()
        {
            if (_configured) return;
            _configured = true;

            DOTween.Init(recycleAllByDefault: true);
            DOTween.defaultRecyclable = true; // also covers the case where DOTween had already been initialized
            DOTween.SetTweensCapacity(TweenerCapacity, SequenceCapacity);
        }
    }
}
