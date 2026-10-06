namespace Match3.Game
{
    /// <summary>
    /// Where progress is kept. ProgressService only knows this interface, so tests use an in-memory store
    /// and the game uses a file, without ProgressService changing.
    /// </summary>
    public interface IProgressStore
    {
        /// <summary>Always returns something usable: fresh progress if nothing was saved or the save is unreadable.</summary>
        ProgressData Load();

        void Save(ProgressData data);

        /// <summary>Forgets everything that was saved.</summary>
        void Clear();
    }
}
