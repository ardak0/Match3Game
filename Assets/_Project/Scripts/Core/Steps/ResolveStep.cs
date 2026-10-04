namespace Match3.Core
{
    /// <summary>
    /// One thing that happened while the board resolved a swap. The model produces a list of these,
    /// instantly, and the view plays them back one after another with animations.
    ///
    /// Wave 0 is the player's swap. Waves 1, 2, 3... are the rounds of "clear, fall, refill".
    /// A second round only exists when the refill or the fall created a new match (a cascade).
    /// The view can use the wave number to run each round as one animation sequence.
    ///
    /// Steps are plain data. They never touch the board.
    /// </summary>
    public abstract class ResolveStep
    {
        public int Wave { get; }

        protected ResolveStep(int wave)
        {
            Wave = wave;
        }
    }
}
