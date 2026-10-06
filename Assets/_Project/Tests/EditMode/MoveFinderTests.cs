using System.Collections.Generic;
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
    
        // ---------- GetAllMoves ----------

        [Test]
        public void GetAllMoves_ListsEveryValidSwapOnce()
        {
            // Only one swap makes a match: the Blue and the last Red give R R R B.
            Board board = TestBoards.FromRows("RRBR");
            List<SwapMove> moves = new List<SwapMove>();

            _moveFinder.GetAllMoves(board, moves);

            Assert.That(moves.Count, Is.EqualTo(1));
            Assert.That(moves[0].A, Is.EqualTo(new GridPos(2, 0)));
            Assert.That(moves[0].B, Is.EqualTo(new GridPos(3, 0)));
        }

        [Test]
        public void GetAllMoves_ClearsTheListFirst_AndIsEmptyOnADeadBoard()
        {
            Board board = TestBoards.FromRows("RGB", "GBR", "BRG");
            List<SwapMove> moves = new List<SwapMove> { new SwapMove(new GridPos(0, 0), new GridPos(1, 0)) };

            _moveFinder.GetAllMoves(board, moves);

            Assert.That(moves, Is.Empty);
        }

        [Test]
        public void GetAllMoves_AgreesWithIsValidMove_ForEveryAdjacentPair()
        {
            Board board = TestBoards.FromRows("RRBRG", "GBRBB", "RRGRY", "BGBYY").WithSpecial(0, 0, SpecialType.Bomb);
            List<SwapMove> moves = new List<SwapMove>();
            _moveFinder.GetAllMoves(board, moves);

            int valid = 0;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (x + 1 < board.Width && _moveFinder.IsValidMove(board, new GridPos(x, y), new GridPos(x + 1, y))) valid++;
                    if (y + 1 < board.Height && _moveFinder.IsValidMove(board, new GridPos(x, y), new GridPos(x, y + 1))) valid++;
                }
            }

            Assert.That(moves.Count, Is.EqualTo(valid));
            Assert.That(moves.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GetAllMoves_SkipsChainedTiles()
        {
            // The only match would need the chained Blue to move.
            Board board = TestBoards.FromRows("RRBR").WithObstacles("..L.");
            List<SwapMove> moves = new List<SwapMove>();

            _moveFinder.GetAllMoves(board, moves);

            Assert.That(moves, Is.Empty);
        }
    }
}
