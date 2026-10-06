using System;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class ProgressServiceTests
    {
        // Every test level asks for 5 moves left for 2 stars and 8 for 3 stars.
        private static StarThresholds[] Thresholds(int levelCount)
        {
            StarThresholds[] thresholds = new StarThresholds[levelCount];
            for (int i = 0; i < levelCount; i++) thresholds[i] = new StarThresholds(5, 8);
            return thresholds;
        }

        private static ProgressService NewService(IProgressStore store, int levelCount = 3)
        {
            return new ProgressService(store, Thresholds(levelCount));
        }

        // ---------- StarThresholds ----------

        [Test]
        public void Stars_BelowTwoStarThreshold_IsOneStar()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(4), Is.EqualTo(1));
        }

        [Test]
        public void Stars_ZeroMovesLeft_IsStillOneStar()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(0), Is.EqualTo(1));
        }

        [Test]
        public void Stars_ExactlyAtTwoStarThreshold_IsTwoStars()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(5), Is.EqualTo(2));
        }

        [Test]
        public void Stars_OneBelowThreeStarThreshold_IsTwoStars()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(7), Is.EqualTo(2));
        }

        [Test]
        public void Stars_ExactlyAtThreeStarThreshold_IsThreeStars()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(8), Is.EqualTo(3));
        }

        [Test]
        public void Stars_AboveThreeStarThreshold_IsThreeStars()
        {
            Assert.That(new StarThresholds(5, 8).GetStars(20), Is.EqualTo(3));
        }

        // ---------- Unlocking ----------

        [Test]
        public void FreshProgress_OnlyTheFirstLevelIsUnlocked()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            Assert.That(progress.IsUnlocked(0), Is.True);
            Assert.That(progress.IsUnlocked(1), Is.False);
            Assert.That(progress.IsUnlocked(2), Is.False);
        }

        [Test]
        public void FreshProgress_HasNoStarsAndTheFirstLevelIsNext()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            Assert.That(progress.TotalStars, Is.EqualTo(0));
            Assert.That(progress.GetStars(0), Is.EqualTo(0));
            Assert.That(progress.NextLevelToPlay, Is.EqualTo(0));
        }

        [Test]
        public void IndexesOutsideTheLevelList_AreLockedWithNoStars()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            Assert.That(progress.IsUnlocked(-1), Is.False);
            Assert.That(progress.IsUnlocked(3), Is.False);
            Assert.That(progress.GetStars(-1), Is.EqualTo(0));
            Assert.That(progress.GetStars(3), Is.EqualTo(0));
        }

        [Test]
        public void Winning_UnlocksTheNextLevel()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            WinResult result = progress.RecordWin(0, 2);

            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(result.UnlockedNextLevel, Is.True);
        }

        [Test]
        public void Winning_TheSameLevelAgain_DoesNotReportAnUnlock()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 2);

            WinResult second = progress.RecordWin(0, 2);

            Assert.That(second.UnlockedNextLevel, Is.False);
        }

        [Test]
        public void Winning_TheLastLevel_UnlocksNothingAndDoesNotThrow()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 0);
            progress.RecordWin(1, 0);

            WinResult result = progress.RecordWin(2, 0);

            Assert.That(result.UnlockedNextLevel, Is.False);
            Assert.That(progress.IsUnlocked(3), Is.False);
        }

        [Test]
        public void NextLevelToPlay_IsTheFirstLevelNotWonYet()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            progress.RecordWin(0, 2);
            Assert.That(progress.NextLevelToPlay, Is.EqualTo(1));

            progress.RecordWin(1, 2);
            Assert.That(progress.NextLevelToPlay, Is.EqualTo(2));
        }

        [Test]
        public void NextLevelToPlay_WhenEverythingIsWon_IsTheLastLevel()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 2);
            progress.RecordWin(1, 2);
            progress.RecordWin(2, 2);

            Assert.That(progress.NextLevelToPlay, Is.EqualTo(2));
        }

        // ---------- Stars ----------

        [Test]
        public void RecordWin_StoresTheStarsForThatLevel()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            WinResult result = progress.RecordWin(0, 8);

            Assert.That(result.Stars, Is.EqualTo(3));
            Assert.That(progress.GetStars(0), Is.EqualTo(3));
        }

        [Test]
        public void RecordWin_BetterRun_RaisesTheStarsAndIsANewBest()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 1);

            WinResult better = progress.RecordWin(0, 6);

            Assert.That(better.PreviousBestStars, Is.EqualTo(1));
            Assert.That(better.Stars, Is.EqualTo(2));
            Assert.That(better.IsNewBest, Is.True);
            Assert.That(progress.GetStars(0), Is.EqualTo(2));
        }

        [Test]
        public void RecordWin_WorseRun_NeverLowersTheStars()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 9);

            WinResult worse = progress.RecordWin(0, 1);

            Assert.That(worse.Stars, Is.EqualTo(1), "the result describes this run");
            Assert.That(worse.IsNewBest, Is.False);
            Assert.That(progress.GetStars(0), Is.EqualTo(3), "the saved best is untouched");
        }

        [Test]
        public void RecordWin_SameStarsAgain_IsNotANewBest()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 6);

            Assert.That(progress.RecordWin(0, 7).IsNewBest, Is.False);
        }

        [Test]
        public void RecordWin_FirstWinIsAlwaysANewBest()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            Assert.That(progress.RecordWin(0, 0).IsNewBest, Is.True);
        }

        [Test]
        public void TotalStars_IsTheSumOverAllLevels()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());
            progress.RecordWin(0, 9); // 3 stars
            progress.RecordWin(1, 5); // 2 stars
            progress.RecordWin(2, 0); // 1 star

            Assert.That(progress.TotalStars, Is.EqualTo(6));
            Assert.That(progress.MaxStars, Is.EqualTo(9));
        }

        [Test]
        public void RecordWin_UnknownLevelIndex_Throws()
        {
            ProgressService progress = NewService(new InMemoryProgressStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(3, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(-1, 5));
        }

        [Test]
        public void EachLevelUsesItsOwnThresholds()
        {
            StarThresholds[] thresholds = { new StarThresholds(2, 4), new StarThresholds(10, 15) };
            ProgressService progress = new ProgressService(new InMemoryProgressStore(), thresholds);

            Assert.That(progress.RecordWin(0, 4).Stars, Is.EqualTo(3));
            Assert.That(progress.RecordWin(1, 4).Stars, Is.EqualTo(1));
        }

        // ---------- Saving and loading ----------

        [Test]
        public void Progress_SurvivesAFreshServiceOnTheSameStore()
        {
            InMemoryProgressStore store = new InMemoryProgressStore();
            NewService(store).RecordWin(0, 9);
            NewService(store).RecordWin(1, 5);

            ProgressService reloaded = NewService(store);

            Assert.That(reloaded.GetStars(0), Is.EqualTo(3));
            Assert.That(reloaded.GetStars(1), Is.EqualTo(2));
            Assert.That(reloaded.IsUnlocked(2), Is.True);
            Assert.That(reloaded.TotalStars, Is.EqualTo(5));
        }

        [Test]
        public void RecordWin_SavesWhenTheBestImproves()
        {
            InMemoryProgressStore store = new InMemoryProgressStore();
            ProgressService progress = NewService(store);

            progress.RecordWin(0, 1);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            progress.RecordWin(0, 1); // nothing improved
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MoreLevelsAddedLater_KeepOldProgress()
        {
            InMemoryProgressStore store = new InMemoryProgressStore();
            NewService(store, 2).RecordWin(0, 9);
            NewService(store, 2).RecordWin(1, 9);

            ProgressService grown = NewService(store, 4);

            Assert.That(grown.GetStars(1), Is.EqualTo(3));
            Assert.That(grown.IsUnlocked(2), Is.True);
            Assert.That(grown.IsUnlocked(3), Is.False);
        }

        [Test]
        public void ImpossibleStoredStars_AreClampedToTheValidRange()
        {
            InMemoryProgressStore store = new InMemoryProgressStore();
            store.Save(new ProgressData { stars = new[] { 99, -4, 2 } });

            ProgressService progress = NewService(store);

            Assert.That(progress.GetStars(0), Is.EqualTo(3));
            Assert.That(progress.GetStars(1), Is.EqualTo(0));
            Assert.That(progress.GetStars(2), Is.EqualTo(2));
        }

        [Test]
        public void ResetProgress_ClearsStarsAndLocksLevelsAgain()
        {
            InMemoryProgressStore store = new InMemoryProgressStore();
            ProgressService progress = NewService(store);
            progress.RecordWin(0, 9);

            progress.Reset();

            Assert.That(progress.TotalStars, Is.EqualTo(0));
            Assert.That(progress.IsUnlocked(1), Is.False);
            Assert.That(NewService(store).TotalStars, Is.EqualTo(0), "the store was cleared too");
        }
    }
}
