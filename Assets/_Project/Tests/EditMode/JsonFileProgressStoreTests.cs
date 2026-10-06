using System.IO;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class JsonFileProgressStoreTests
    {
        private string _folder;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "match3_progress_test_" + System.Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "progress.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [Test]
        public void MissingFile_GivesFreshProgress()
        {
            ProgressData data = new JsonFileProgressStore(_path).Load();

            Assert.That(data, Is.Not.Null);
            Assert.That(data.stars.Length, Is.EqualTo(0));
            Assert.That(data.version, Is.EqualTo(ProgressData.CurrentVersion));
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            JsonFileProgressStore store = new JsonFileProgressStore(_path);
            store.Save(new ProgressData { stars = new[] { 3, 2, 1, 0 } });

            ProgressData loaded = new JsonFileProgressStore(_path).Load();

            Assert.That(loaded.stars, Is.EqualTo(new[] { 3, 2, 1, 0 }));
        }

        [Test]
        public void Save_CreatesTheFolderAndLeavesNoTempFileBehind()
        {
            new JsonFileProgressStore(_path).Save(new ProgressData { stars = new[] { 1 } });

            Assert.That(File.Exists(_path), Is.True);
            Assert.That(File.Exists(_path + ".tmp"), Is.False);
        }

        [Test]
        public void Save_Twice_KeepsTheLatest()
        {
            JsonFileProgressStore store = new JsonFileProgressStore(_path);
            store.Save(new ProgressData { stars = new[] { 1 } });
            store.Save(new ProgressData { stars = new[] { 3, 3 } });

            Assert.That(store.Load().stars, Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void CorruptFile_FallsBackToFreshProgress()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "{ this is not json at all <<<");

            ProgressData data = new JsonFileProgressStore(_path).Load();

            Assert.That(data, Is.Not.Null);
            Assert.That(data.stars.Length, Is.EqualTo(0));
        }

        [Test]
        public void EmptyFile_FallsBackToFreshProgress()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "");

            Assert.That(new JsonFileProgressStore(_path).Load().stars.Length, Is.EqualTo(0));
        }

        [Test]
        public void FileFromANewerVersion_FallsBackToFreshProgress()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "{\"version\":999,\"stars\":[3,3,3]}");

            Assert.That(new JsonFileProgressStore(_path).Load().stars.Length, Is.EqualTo(0));
        }

        [Test]
        public void ValidJsonWithoutTheStarsList_FallsBackToFreshProgress()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "{\"version\":1}");

            ProgressData data = new JsonFileProgressStore(_path).Load();

            Assert.That(data.stars, Is.Not.Null);
            Assert.That(data.stars.Length, Is.EqualTo(0));
        }

        [Test]
        public void SavedFile_CarriesTheVersionNumber()
        {
            new JsonFileProgressStore(_path).Save(new ProgressData { stars = new[] { 1 } });

            Assert.That(File.ReadAllText(_path), Does.Contain("\"version\":" + ProgressData.CurrentVersion));
        }

        [Test]
        public void Clear_DeletesTheFile_AndIsSafeWhenThereIsNone()
        {
            JsonFileProgressStore store = new JsonFileProgressStore(_path);
            store.Clear(); // nothing there yet: must not throw

            store.Save(new ProgressData { stars = new[] { 2 } });
            store.Clear();

            Assert.That(File.Exists(_path), Is.False);
            Assert.That(store.Load().stars.Length, Is.EqualTo(0));
        }

        [Test]
        public void CorruptFile_ThenSave_WorksAgain()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "garbage");
            JsonFileProgressStore store = new JsonFileProgressStore(_path);

            store.Save(new ProgressData { stars = new[] { 2 } });

            Assert.That(store.Load().stars, Is.EqualTo(new[] { 2 }));
        }
    }
}
