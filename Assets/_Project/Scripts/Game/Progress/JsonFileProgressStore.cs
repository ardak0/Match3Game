using System;
using System.IO;
using UnityEngine;

namespace Match3.Game
{
    /// <summary>
    /// Saves progress as a small JSON file. The caller gives the path (the game passes
    /// Application.persistentDataPath + "/progress.json"), so tests can use a temp folder.
    /// Loading never throws: a missing, empty, corrupt or newer-version file just means fresh progress.
    /// </summary>
    public sealed class JsonFileProgressStore : IProgressStore
    {
        private readonly string _path;

        public JsonFileProgressStore(string path)
        {
            _path = path;
        }

        public ProgressData Load()
        {
            if (!File.Exists(_path)) return new ProgressData();

            try
            {
                string json = File.ReadAllText(_path);
                ProgressData data = JsonUtility.FromJson<ProgressData>(json);

                if (data == null || data.stars == null || data.version != ProgressData.CurrentVersion)
                {
                    Debug.LogWarning("Progress file has an unexpected shape or version. Starting with fresh progress.");
                    return new ProgressData();
                }

                return data;
            }
            catch (Exception exception) // not valid JSON, or the file could not be read
            {
                Debug.LogWarning("Progress file could not be read (" + exception.Message + "). Starting with fresh progress.");
                return new ProgressData();
            }
        }

        public void Save(ProgressData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));

                // Write a temporary file first and copy it over the real one, so a crash in the middle of
                // writing can never leave a half-written progress file.
                string temporaryPath = _path + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data));
                File.Copy(temporaryPath, _path, true);
                File.Delete(temporaryPath);
            }
            catch (Exception exception) // disk full, no permission, ...
            {
                Debug.LogWarning("Progress could not be saved (" + exception.Message + ").");
            }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(_path)) File.Delete(_path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Progress file could not be deleted (" + exception.Message + ").");
            }
        }
    }
}
