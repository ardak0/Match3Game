using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class GravityResolverTests
    {
        private GravityResolver _gravity;

        [SetUp]
        public void SetUp()
        {
            _gravity = new GravityResolver();
        }

        private static TileMove FindMove(FallStep step, int tileId)
        {
            for (int i = 0; i < step.Moves.Count; i++)
            {
                if (step.Moves[i].TileId == tileId) return step.Moves[i];
            }

            Assert.Fail("No move recorded for tile " + tileId);
            return default;
        }

        [Test]
        public void TilesFallDown_ToCloseGaps()
        {
            Board board = TestBoards.FromRows(
                "R.G",
                "..B",
                "G..",
                "...");
            Tile g = board.Get(0, 1);
            Tile r = board.Get(0, 3);
            Tile b = board.Get(2, 2);
            Tile g2 = board.Get(2, 3);

            FallStep step = _gravity.Apply(board, 1);

            // column 0: G and R drop to the bottom, keeping their order
            Assert.That(board.Get(0, 0), Is.SameAs(g));
            Assert.That(board.Get(0, 1), Is.SameAs(r));
            Assert.That(board.Get(0, 2), Is.Null);
            Assert.That(board.Get(0, 3), Is.Null);
            // column 1 was empty and stays empty
            for (int y = 0; y < 4; y++)
            {
                Assert.That(board.Get(1, y), Is.Null);
            }

            // column 2: B and the second G drop to the bottom
            Assert.That(board.Get(2, 0), Is.SameAs(b));
            Assert.That(board.Get(2, 1), Is.SameAs(g2));
            Assert.That(board.Get(2, 2), Is.Null);
            Assert.That(board.Get(2, 3), Is.Null);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Wave, Is.EqualTo(1));
            Assert.That(step.Moves.Count, Is.EqualTo(4));
        }

        [Test]
        public void Moves_RecordTheTileIdAndWhereItWentFromAndTo()
        {
            Board board = TestBoards.FromRows(
                "R",
                ".",
                ".",
                "G");
            Tile r = board.Get(0, 3);

            FallStep step = _gravity.Apply(board, 1);

            Assert.That(step.Moves.Count, Is.EqualTo(1)); // G is already on the floor
            TileMove move = FindMove(step, r.Id);
            Assert.That(move.From, Is.EqualTo(new GridPos(0, 3)));
            Assert.That(move.To, Is.EqualTo(new GridPos(0, 1)));
        }

        [Test]
        public void OnlyTilesAboveAGapMove()
        {
            Board board = TestBoards.FromRows(
                "R",
                "G",
                ".",
                "B");
            Tile b = board.Get(0, 0);

            FallStep step = _gravity.Apply(board, 1);

            Assert.That(step.Moves.Count, Is.EqualTo(2)); // G and R fall one cell; B stays
            Assert.That(board.Get(0, 0), Is.SameAs(b));
            Assert.That(board.Get(0, 1).Color, Is.EqualTo(TileColor.Green));
            Assert.That(board.Get(0, 2).Color, Is.EqualTo(TileColor.Red));
            Assert.That(board.Get(0, 3), Is.Null);
        }

        [Test]
        public void FullBoard_ReturnsNullAndChangesNothing()
        {
            Board board = TestBoards.FromRows("RG", "GR");
            Tile before = board.Get(0, 1);

            FallStep step = _gravity.Apply(board, 1);

            Assert.That(step, Is.Null);
            Assert.That(board.Get(0, 1), Is.SameAs(before));
        }

        [Test]
        public void GapsOnlyAtTheTop_ReturnsNull()
        {
            Board board = TestBoards.FromRows("..", "..", "RG");

            Assert.That(_gravity.Apply(board, 1), Is.Null);
        }

        [Test]
        public void EmptyBoard_ReturnsNull()
        {
            Board board = TestBoards.FromRows("...", "...");

            Assert.That(_gravity.Apply(board, 1), Is.Null);
        }

        [Test]
        public void Gravity_NeverMovesTilesSideways()
        {
            Board board = TestBoards.FromRows(
                "R.B",
                "...");

            FallStep step = _gravity.Apply(board, 1);

            for (int i = 0; i < step.Moves.Count; i++)
            {
                Assert.That(step.Moves[i].To.X, Is.EqualTo(step.Moves[i].From.X));
            }
        }

        [Test]
        public void CallingTwice_SecondCallFindsNothingToDo()
        {
            Board board = TestBoards.FromRows("R.", "..", ".G");

            Assert.That(_gravity.Apply(board, 1), Is.Not.Null);
            Assert.That(_gravity.Apply(board, 1), Is.Null);
        }
    }
}
