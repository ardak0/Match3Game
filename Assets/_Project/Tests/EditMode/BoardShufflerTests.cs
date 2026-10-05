using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class BoardShufflerTests
    {
        private static BoardShuffler NewShuffler(int seed, out MoveFinder moveFinder, out MatchFinder matchFinder)
        {
            matchFinder = new MatchFinder();
            moveFinder = new MoveFinder(matchFinder);
            return new BoardShuffler(new SystemRandom(seed), moveFinder);
        }

        // A 6x6 board with 6 colors in a diagonal pattern: no match, and no swap makes one.
        private static Board DeadBoard()
        {
            return TestBoards.Diagonal(6, 6, 6);
        }

        [Test]
        public void DeadBoard_ReallyHasNoPossibleMove_Precondition()
        {
            NewShuffler(1, out MoveFinder moveFinder, out _);
            Assert.IsFalse(moveFinder.HasPossibleMove(DeadBoard()));
        }

        [Test]
        public void Shuffle_OfDeadBoard_GivesBoardWithAMove_AndNoMatches()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                BoardShuffler shuffler = NewShuffler(seed, out MoveFinder moveFinder, out MatchFinder matchFinder);
                Board board = DeadBoard();

                bool ok = shuffler.TryShuffle(board, out ShuffleStep step);

                Assert.IsTrue(ok, "seed " + seed);
                Assert.IsNotNull(step);
                Assert.IsTrue(moveFinder.HasPossibleMove(board), "seed " + seed);

                List<Match> matches = new List<Match>();
                matchFinder.FindMatches(board, matches);
                Assert.AreEqual(0, matches.Count, "seed " + seed);
            }
        }

        [Test]
        public void Shuffle_KeepsTheSameTiles()
        {
            BoardShuffler shuffler = NewShuffler(5, out _, out _);
            Board board = DeadBoard();
            HashSet<int> idsBefore = IdsOf(board);

            shuffler.TryShuffle(board, out _);

            CollectionAssert.AreEquivalent(idsBefore, IdsOf(board));
            Assert.AreEqual(36, IdsOf(board).Count);
        }

        [Test]
        public void Shuffle_KeepsSpecialTiles()
        {
            BoardShuffler shuffler = NewShuffler(5, out _, out _);
            Board board = DeadBoard().WithSpecial(2, 2, SpecialType.Bomb);
            Tile bomb = board.Get(2, 2);

            bool ok = shuffler.TryShuffle(board, out _);

            Assert.IsTrue(ok);
            bool found = false;
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    if (board.Get(x, y) == bomb) found = true;
                }
            }

            Assert.IsTrue(found, "the bomb tile must still be on the board");
        }

        [Test]
        public void Moves_DescribeExactlyWhatChanged()
        {
            BoardShuffler shuffler = NewShuffler(9, out _, out _);
            Board board = DeadBoard();

            // Remember where each tile was.
            Dictionary<int, GridPos> before = new Dictionary<int, GridPos>();
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    before[board.Get(x, y).Id] = new GridPos(x, y);
                }
            }

            shuffler.TryShuffle(board, out ShuffleStep step);

            HashSet<int> moved = new HashSet<int>();
            foreach (TileMove move in step.Moves)
            {
                Assert.AreEqual(before[move.TileId], move.From);
                Assert.AreEqual(move.TileId, board.Get(move.To).Id);
                Assert.AreNotEqual(move.From, move.To);
                moved.Add(move.TileId);
            }

            // Every tile NOT listed must still be in its old cell.
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    int id = board.Get(x, y).Id;
                    if (!moved.Contains(id)) Assert.AreEqual(before[id], new GridPos(x, y));
                }
            }

            Assert.AreEqual(0, step.Wave);
        }

        [Test]
        public void Shuffle_WhenNothingWorks_ReturnsFalse_AndLeavesBoardUnchanged()
        {
            // All one color: every arrangement is a match, so no valid shuffle exists.
            BoardShuffler shuffler = NewShuffler(1, out _, out _);
            Board board = TestBoards.FromRows("RRRR", "RRRR", "RRRR", "RRRR");
            Tile[] before = new Tile[16];
            for (int i = 0; i < 16; i++) before[i] = board.Get(i % 4, i / 4);

            bool ok = shuffler.TryShuffle(board, out ShuffleStep step);

            Assert.IsFalse(ok);
            Assert.IsNull(step);
            for (int i = 0; i < 16; i++) Assert.AreSame(before[i], board.Get(i % 4, i / 4));
        }

        [Test]
        public void SameSeed_GivesSameShuffle()
        {
            Board a = DeadBoard();
            Board b = DeadBoard();
            NewShuffler(77, out _, out _).TryShuffle(a, out _);
            NewShuffler(77, out _, out _).TryShuffle(b, out _);

            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    Assert.AreEqual(a.Get(x, y).Color, b.Get(x, y).Color);
                    Assert.AreEqual(a.Get(x, y).Id, b.Get(x, y).Id);
                }
            }
        }

        [Test]
        public void Shuffle_OfBoardWithEmptyCell_Throws()
        {
            BoardShuffler shuffler = NewShuffler(1, out _, out _);
            Board board = TestBoards.FromRows("RGB", "R.B", "RGB");

            Assert.Throws<InvalidOperationException>(() => shuffler.TryShuffle(board, out _));
        }

        private static HashSet<int> IdsOf(Board board)
        {
            HashSet<int> ids = new HashSet<int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ids.Add(board.Get(x, y).Id);
                }
            }

            return ids;
        }
    }
}
