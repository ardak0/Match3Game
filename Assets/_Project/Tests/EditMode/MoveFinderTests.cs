using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class MoveFinderTests
    {
        private MoveFinder _moveFinder;

        [SetUp]
        public void SetUp()
        {
            _moveFinder = new MoveFinder(new MatchFinder());
        }

        [Test]
        public void HasPossibleMove_TrueWhenOneSwapMakesThree()
        {
            // Swapping the Blue and the last Red gives R R R B.
            Board board = TestBoards.FromRows("RRBR");

            Assert.That(_moveFinder.HasPossibleMove(board), Is.True);
        }

        [Test]
        public void HasPossibleMove_FalseOnADeadBoard()
        {
            // Every row and column holds three different colors, so no single swap can line up three.
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BRG");

            Assert.That(_moveFinder.HasPossibleMove(board), Is.False);
        }

        [Test]
        public void HasPossibleMove_LeavesTheBoardUnchanged()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BRG");
            Tile bottomLeft = board.Get(0, 0);
            Tile center = board.Get(1, 1);

            _moveFinder.HasPossibleMove(board);

            Assert.That(board.Get(0, 0), Is.SameAs(bottomLeft));
            Assert.That(board.Get(1, 1), Is.SameAs(center));
        }

        [Test]
        public void WouldMatchAfterSwap_TrueForAGoodSwap()
        {
            Board board = TestBoards.FromRows("RRBR");

            Assert.That(_moveFinder.WouldMatchAfterSwap(board, new GridPos(2, 0), new GridPos(3, 0)), Is.True);
        }

        [Test]
        public void WouldMatchAfterSwap_FalseForABadSwap()
        {
            Board board = TestBoards.FromRows("RRBR");

            Assert.That(_moveFinder.WouldMatchAfterSwap(board, new GridPos(0, 0), new GridPos(1, 0)), Is.False);
        }

        [Test]
        public void WouldMatchAfterSwap_TrueForAVerticalSwap()
        {
            // Swapping the Green at (0,2) with the Red above it puts three Reds in column 0.
            Board board = TestBoards.FromRows(
                "RGB",
                "GBG",
                "RGB",
                "RBG");

            Assert.That(_moveFinder.WouldMatchAfterSwap(board, new GridPos(0, 2), new GridPos(0, 3)), Is.True);
        }

        [Test]
        public void WouldMatchAfterSwap_NeverCountsAnEmptyCell()
        {
            Board board = TestBoards.FromRows("RR.R");

            Assert.That(_moveFinder.WouldMatchAfterSwap(board, new GridPos(2, 0), new GridPos(3, 0)), Is.False);
        }

        [Test]
        public void TryFindMove_ReturnsASwapThatReallyMakesAMatch()
        {
            Board board = TestBoards.FromRows("RRBR");

            bool found = _moveFinder.TryFindMove(board, out GridPos a, out GridPos b);

            Assert.That(found, Is.True);
            Assert.That(_moveFinder.WouldMatchAfterSwap(board, a, b), Is.True);
        }

        [Test]
        public void TryFindMove_FalseOnADeadBoard()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BRG");

            Assert.That(_moveFinder.TryFindMove(board, out _, out _), Is.False);
        }

        [Test]
        public void ASwapWithASpecialTile_IsAValidMove_EvenWhenNoMatchIsMade()
        {
            // The same dead board as above, with one special tile on it.
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BRG").WithSpecial(1, 1, SpecialType.Bomb);

            Assert.That(_moveFinder.HasPossibleMove(board), Is.True);
            Assert.That(_moveFinder.IsValidMove(board, new GridPos(1, 1), new GridPos(2, 1)), Is.True);
            Assert.That(_moveFinder.IsValidMove(board, new GridPos(0, 0), new GridPos(1, 0)), Is.False);
        }

        [Test]
        public void ASwapWithAnEmptyCell_IsNeverAValidMove_EvenForASpecial()
        {
            Board board = TestBoards.FromRows("RR.").WithSpecial(1, 0, SpecialType.Bomb);

            Assert.That(_moveFinder.IsValidMove(board, new GridPos(1, 0), new GridPos(2, 0)), Is.False);
        }
    }
}
