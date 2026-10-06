using System.IO;
using Match3.Data;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// The one place that decides where progress is saved and how a ProgressService is put together for the real game.
    /// The Home scene, the Game scene and the editor's Reset Progress menu all use it, so they always agree.
    /// </summary>
    public static class ProgressSetup
    {
        private const string FileName = "progress.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static IProgressStore CreateStore()
        {
            return new JsonFileProgressStore(FilePath);
        }

        public static ProgressService Create(LevelCatalog catalog)
        {
            return new ProgressService(CreateStore(), catalog.GetStarThresholds());
        }
    }
}
