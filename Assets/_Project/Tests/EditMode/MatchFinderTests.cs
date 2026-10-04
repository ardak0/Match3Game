using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class MatchFinderTests
    {
        private MatchFinder _finder;
        private List<Match> _matches;

        [SetUp]
        public void SetUp()
        {
            _finder = new MatchFinder();
            _matches = new List<Match>();
        }

        private List<Match> Find(Board board)
        {
            _finder.FindMatches(board, _matches);
            return _matches;
        }

        private static bool Has(Match match, int x, int y)
        {
            return match.Positions.Contains(new GridPos(x, y));
        }

        // ---------- straight runs ----------

        [Test]
        public void Horizontal3_IsOneMatch()
        {
            Board board = TestBoards.FromRows(
                "BGBGB",
                "RRRGB");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Color, Is.EqualTo(TileColor.Red));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(3));
            Assert.That(Has(matches[0], 0, 0) && Has(matches[0], 1, 0) && Has(matches[0], 2, 0), Is.True);
        }

        [Test]
        public void Horizontal4_IsOneMatchOfFour()
        {
            Board board = TestBoards.FromRows(
                "BGBGBG",
                "RRRRGB");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(4));
        }

        [Test]
        public void Horizontal5_IsOneMatchOfFive()
        {
            Board board = TestBoards.FromRows(
                "GBGBG",
                "RRRRR");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(5));
        }

        [Test]
        public void Vertical3_IsOneMatch()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "RBG",
                "RGB");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Color, Is.EqualTo(TileColor.Red));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(3));
            Assert.That(Has(matches[0], 0, 0) && Has(matches[0], 0, 1) && Has(matches[0], 0, 2), Is.True);
        }

        [Test]
        public void Vertical4_IsOneMatchOfFour()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "RBG",
                "RGB",
                "RBG");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(4));
        }

        // ---------- L, T and + shapes are merged ----------

        [Test]
        public void LShape_IsMergedIntoOneMatch()
        {
            Board board = TestBoards.FromRows(
                "RGBGB",
                "RBGBG",
                "RRRGB");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Color, Is.EqualTo(TileColor.Red));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(5)); // corner tile counted once, not twice
        }

        [Test]
        public void TShape_IsMergedIntoOneMatch()
        {
            Board board = TestBoards.FromRows(
                "RRR",
                "GRB",
                "BRG");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(5));
        }

        [Test]
        public void PlusShape_IsMergedIntoOneMatch()
        {
            Board board = TestBoards.FromRows(
                "BRG",
                "RRR",
                "GRB");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(5));
        }

        // ---------- separate matches stay separate ----------

        [Test]
        public void TwoRunsOfSameColorWithGapBetween_AreTwoMatches()
        {
            Board board = TestBoards.FromRows("RRRBRRR");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(3));
            Assert.That(matches[1].Positions.Count, Is.EqualTo(3));
        }

        [Test]
        public void TouchingRunsOfDifferentColors_AreTwoMatches()
        {
            Board board = TestBoards.FromRows("RRRGGG");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches[0].Color, Is.EqualTo(TileColor.Red));
            Assert.That(matches[1].Color, Is.EqualTo(TileColor.Green));
        }

        [Test]
        public void StackedRunsThatOnlyTouch_AreTwoMatches()
        {
            // Two parallel runs of the same color share no tile, so they do not merge.
            Board board = TestBoards.FromRows(
                "RRR",
                "RRR");

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(2));
        }

        // ---------- no false positives ----------

        [Test]
        public void TwoInARow_IsNotAMatch()
        {
            Assert.That(Find(TestBoards.FromRows("RRGRR")), Is.Empty);
        }

        [Test]
        public void TwoByTwoBlock_IsNotAMatch()
        {
            Assert.That(Find(TestBoards.FromRows("RR", "RR")), Is.Empty);
        }

        [Test]
        public void SmallLOfThreeTiles_IsNotAMatch()
        {
            // Three red tiles forming a corner are not a line of 3.
            Assert.That(Find(TestBoards.FromRows("RG", "RR")), Is.Empty);
        }

        [Test]
        public void CheckerPattern_IsNotAMatch()
        {
            Assert.That(Find(TestBoards.FromRows("RGRG", "GRGR")), Is.Empty);
        }

        [Test]
        public void ThreeDifferentColors_IsNotAMatch()
        {
            Assert.That(Find(TestBoards.FromRows("RGB")), Is.Empty);
        }

        [Test]
        public void EmptyCells_NeverMatch()
        {
            Assert.That(Find(TestBoards.FromRows("...", "...")), Is.Empty);
            Assert.That(Find(TestBoards.FromRows("R.R.R")), Is.Empty);
        }

        [Test]
        public void EmptyCellBreaksARun()
        {
            Assert.That(Find(TestBoards.FromRows("RR.RR")), Is.Empty);
        }

        // ---------- other behavior ----------

        [Test]
        public void SpecialTile_MatchesByItsColor()
        {
            Board board = TestBoards.FromRows("RRR");
            board.Set(1, 0, board.NewTile(TileColor.Red, SpecialType.Bomb));

            List<Match> matches = Find(board);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(3));
        }

        [Test]
        public void FindMatches_ClearsOldResults()
        {
            List<Match> matches = Find(TestBoards.FromRows("RRR"));
            Assert.That(matches.Count, Is.EqualTo(1));

            matches = Find(TestBoards.FromRows("RGB"));

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void SameFinder_WorksOnBoardsOfDifferentSizes()
        {
            Assert.That(Find(TestBoards.FromRows("GBGBG", "RRRRR")).Count, Is.EqualTo(1));
            Assert.That(Find(TestBoards.FromRows("RGB", "RBG", "RGB")).Count, Is.EqualTo(1));
            Assert.That(Find(TestBoards.FromRows("RRR")).Count, Is.EqualTo(1));
        }

        [Test]
        public void FindMatches_DoesNotChangeTheBoard()
        {
            Board board = TestBoards.FromRows(
                "RGBGB",
                "RBGBG",
                "RRRGB");
            Tile before = board.Get(0, 0);

            Find(board);

            Assert.That(board.Get(0, 0), Is.SameAs(before));
        }
    }
}
