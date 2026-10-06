using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    /// <summary>Move detection, shuffling and board generation when the board has crates, ice and chained tiles.</summary>
    public class ObstacleShuffleTests
    {
        // A 6x6 board in the diagonal color pattern, with two crates and two chained tiles. The crate cells lose their tiles.
        private static Board LockedBoard()
        {
            return TestBoards.Diagonal(6, 6, 6).WithObstacles(
                "......",
                "...C..",
                ".....L",
                "..C...",
                "......",
                ".L....");
        }

        private static BoardShuffler NewShuffler(int seed, out MoveFinder moveFinder, out MatchFinder matchFinder)
        {
            matchFinder = new MatchFinder();
            moveFinder = new MoveFinder(matchFinder);
            return new BoardShuffler(new SystemRandom(seed), moveFinder);
        }

        // ---------- move detection ----------

        // A 4x2 board with exactly one possible move: swap (2,0) and (3,0) to make three reds in the bottom row.
        private static Board OneMoveBoard()
        {
            return TestBoards.FromRows(
                "BYBY",
                "RRGR");
        }

        [Test]
        public void OneMoveBoard_HasExactlyOneMove_Precondition()
        {
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            Board board = OneMoveBoard();

            Assert.That(moveFinder.IsValidMove(board, new GridPos(2, 0), new GridPos(3, 0)), Is.True);
            Assert.That(moveFinder.TryFindMove(board, out GridPos a, out GridPos b), Is.True);
            Assert.That(a, Is.EqualTo(new GridPos(2, 0)));
            Assert.That(b, Is.EqualTo(new GridPos(3, 0)));
        }

        [Test]
        public void MoveFinder_IgnoresASwapThatNeedsAChainedTile()
        {
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());

            Board chainOnTheMovingTile = OneMoveBoard().WithObstacles("....", "...L");
            Board chainOnTheOtherTile = OneMoveBoard().WithObstacles("....", "..L.");

            Assert.That(moveFinder.IsValidMove(chainOnTheMovingTile, new GridPos(2, 0), new GridPos(3, 0)), Is.False);
            Assert.That(moveFinder.HasPossibleMove(chainOnTheMovingTile), Is.False, "The only move needs a chained tile.");
            Assert.That(moveFinder.HasPossibleMove(chainOnTheOtherTile), Is.False);
        }

        [Test]
        public void MoveFinder_AChainedTileThatIsNotSwapped_DoesNotStopTheMove()
        {
            // The chained tile at (1,0) is part of the match, not one of the swapped tiles.
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            Board board = OneMoveBoard().WithObstacles("....", ".L..");

            Assert.That(moveFinder.HasPossibleMove(board), Is.True);
        }

        [Test]
        public void MoveFinder_NeverSwapsWithACrate()
        {
            // Without the crate, swapping (1,0) with (2,0) would not matter; with it there is no tile to swap at all.
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            Board board = TestBoards.FromRows(
                "BYBY",
                "RR.R").WithObstacles("....", "..C.");

            Assert.That(moveFinder.IsValidMove(board, new GridPos(1, 0), new GridPos(2, 0)), Is.False);
            Assert.That(moveFinder.IsValidMove(board, new GridPos(2, 0), new GridPos(3, 0)), Is.False);
            Assert.That(moveFinder.HasPossibleMove(board), Is.False);
        }

        // ---------- shuffling ----------

        [Test]
        public void Shuffle_NeverMovesCratesOrChainedTiles()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                BoardShuffler shuffler = NewShuffler(seed, out _, out _);
                Board board = LockedBoard();
                Tile chainedA = board.Get(5, 3);
                Tile chainedB = board.Get(1, 0);

                bool ok = shuffler.TryShuffle(board, out ShuffleStep step);

                Assert.That(ok, Is.True, "seed " + seed);
                Assert.That(board.HasCrate(3, 4), Is.True, "seed " + seed);
                Assert.That(board.HasCrate(2, 2), Is.True, "seed " + seed);
                Assert.That(board.Get(3, 4), Is.Null, "seed " + seed);
                Assert.That(board.Get(2, 2), Is.Null, "seed " + seed);
                Assert.That(board.Get(5, 3), Is.SameAs(chainedA), "seed " + seed);
                Assert.That(board.Get(1, 0), Is.SameAs(chainedB), "seed " + seed);
                Assert.That(board.IsChained(5, 3), Is.True, "seed " + seed);
                Assert.That(board.IsChained(1, 0), Is.True, "seed " + seed);

                foreach (TileMove move in step.Moves)
                {
                    Assert.That(move.TileId, Is.Not.EqualTo(chainedA.Id), "seed " + seed);
                    Assert.That(move.TileId, Is.Not.EqualTo(chainedB.Id), "seed " + seed);
                }
            }
        }

        [Test]
        public void Shuffle_KeepsTheSameMovableTiles_AndGivesABoardWithAMoveAndNoMatch()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                BoardShuffler shuffler = NewShuffler(seed, out MoveFinder moveFinder, out MatchFinder matchFinder);
                Board board = LockedBoard();
                HashSet<int> idsBefore = IdsOf(board);

                shuffler.TryShuffle(board, out _);

                Assert.That(IdsOf(board), Is.EquivalentTo(idsBefore), "seed " + seed);
                Assert.That(moveFinder.HasPossibleMove(board), Is.True, "seed " + seed + "\n" + Describe(board));

                List<Match> matches = new List<Match>();
                matchFinder.FindMatches(board, matches);
                Assert.That(matches, Is.Empty, "seed " + seed + "\n" + Describe(board));
            }
        }

        [Test]
        public void Shuffle_LeavesIceInPlace()
        {
            BoardShuffler shuffler = NewShuffler(3, out _, out _);
            Board board = TestBoards.Diagonal(6, 6, 6).WithObstacles(
                "......",
                "......",
                "..J...",
                "......",
                "....I.",
                "......");

            shuffler.TryShuffle(board, out _);

            Assert.That(board.GetObstacle(2, 3), Is.EqualTo(new Obstacle(ObstacleType.Ice, 2)));
            Assert.That(board.GetObstacle(4, 1), Is.EqualTo(new Obstacle(ObstacleType.Ice, 1)));
        }

        [Test]
        public void Shuffle_SkipsAnEmptyCellThatSitsUnderACrate()
        {
            // The cell under a crate can stay empty when nothing can slide into it. The shuffle leaves it alone.
            Board board = TestBoards.Diagonal(6, 6, 6).WithObstacles(
                "......",
                "......",
                "..C...",
                "......",
                "......",
                "......");
            board.Set(2, 3, null);
            BoardShuffler shuffler = NewShuffler(1, out _, out _);

            bool ok = shuffler.TryShuffle(board, out _);

            Assert.That(ok, Is.True);
            Assert.That(board.Get(2, 3), Is.Null);
        }

        [Test]
        public void Shuffle_StillThrowsForAnEmptyCellThatNothingExplains()
        {
            Board board = TestBoards.Diagonal(6, 6, 6);
            board.Set(2, 3, null);
            BoardShuffler shuffler = NewShuffler(1, out _, out _);

            Assert.Throws<InvalidOperationException>(() => shuffler.TryShuffle(board, out _));
        }

        // ---------- generating a board with a layout ----------

        [Test]
        public void Generate_WithALayout_PutsTheObstaclesInPlace_AndStaysMatchFree()
        {
            ObstacleLayout.TryParse(new[]
            {
                "..C....",
                ".I..L..",
                "...D...",
                "J......",
                "...I..L",
                "......."
            }, 7, 6, out ObstacleLayout layout, out string error);
            Assert.That(error, Is.Null);

            for (int seed = 0; seed < 50; seed++)
            {
                Board board = new BoardGenerator(new SystemRandom(seed)).Generate(7, 6, Colors, layout);

                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        Assert.That(board.GetObstacle(x, y), Is.EqualTo(layout.Get(x, y)), "seed " + seed + " cell (" + x + "," + y + ")");
                        bool crate = layout.Get(x, y).Type == ObstacleType.Crate;
                        Assert.That(board.Get(x, y) == null, Is.EqualTo(crate), "seed " + seed + " cell (" + x + "," + y + ")");
                    }
                }

                List<Match> matches = new List<Match>();
                new MatchFinder().FindMatches(board, matches);
                Assert.That(matches, Is.Empty, "seed " + seed + "\n" + Describe(board));
                Assert.That(new MoveFinder(new MatchFinder()).HasPossibleMove(board), Is.True, "seed " + seed);
            }
        }

        [Test]
        public void Generate_WithoutALayout_IsUnchanged()
        {
            Board board = new BoardGenerator(new SystemRandom(4)).Generate(6, 6, Colors);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(board.Get(x, y), Is.Not.Null);
                    Assert.That(board.GetObstacle(x, y).IsNone, Is.True);
                }
            }
        }

        private static HashSet<int> IdsOf(Board board)
        {
            HashSet<int> ids = new HashSet<int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = board.Get(x, y);
                    if (tile != null) ids.Add(tile.Id);
                }
            }

            return ids;
        }
    }
}
