namespace Match3.Game
{
    /// <summary>Keeps progress in memory only. Used by the tests.</summary>
    public sealed class InMemoryProgressStore : IProgressStore
    {
        private ProgressData _saved = new ProgressData();

        /// <summary>How many times Save was called, so a test can check that nothing was saved needlessly.</summary>
        public int SaveCount { get; private set; }

        public ProgressData Load()
        {
            return Copy(_saved);
        }

        public void Save(ProgressData data)
        {
            _saved = Copy(data);
            SaveCount++;
        }

        public void Clear()
        {
            _saved = new ProgressData();
        }

        // Copies in both directions, like a real file would: changing an object you loaded must not change what is saved.
        private static ProgressData Copy(ProgressData data)
        {
            return new ProgressData { version = data.version, stars = (int[])data.stars.Clone() };
        }
    }
}
