namespace Match3.Infrastructure
{
    /// <summary>
    /// The names of the scenes, in one place, so a typo is one compile error instead of a scene that silently does not load.
    /// Both scenes must be in File > Build Profiles (Build Settings), Home first.
    /// </summary>
    public static class SceneNames
    {
        public const string Home = "Home";
        public const string Game = "Game";
    }
}
