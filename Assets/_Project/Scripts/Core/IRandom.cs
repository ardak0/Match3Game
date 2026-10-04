namespace Match3.Core
{
    /// <summary>
    /// The only source of randomness Core is allowed to use.
    /// Injecting it (instead of calling Random directly) makes boards reproducible from a seed,
    /// which is what lets the unit tests and the level seed work.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns an int in [minInclusive, maxExclusive).</summary>
        int Next(int minInclusive, int maxExclusive);
    }
}
