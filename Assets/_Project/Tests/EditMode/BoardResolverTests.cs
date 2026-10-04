using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class BoardResolverTests
    {
        // The five colors used by most tests, in this order: Red=0 Green=1 Blue=2 Yellow=3 Purple=4.
        private static readonly TileColor[] Colors =
        {
            TileColor.Red, TileColor.Green, TileColor.Blue, TileColor.Yellow, TileColor.Purple
        };

        private static Tile[] Snapshot(Board board)
        {
            Tile[] tiles = new Tile[board.Width * board.Height];
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    tiles[y * board.Width + x] = board.Get(x, y);
                }
            }

            return tiles;
        }

        private static void AssertBoardUnchanged(Board board, Tile[] snapshot)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(board.Get(x, y), Is.SameAs(snapshot[y * board.Width + x]),
                        "Cell (" + x + "," + y + ") changed.");
                }
            }
        }

        private static void AssertBoardFullAndStable(Board board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(board.Get(x, y), Is.Not.Null, "Cell (" + x + "," + y + ") is empty.");
                }
            }

            List<Match> matches = new List<Match>();
            new MatchFinder().FindMatches(board, matches);
            Assert.That(matches, Is.Empty, "A match was left on the board.");
        }

        // A random that fails the test if the resolver asks it for anything.
        private static BoardResolver ResolverThatMustNotRefill()
        {
            return new BoardResolver(new ScriptedRandom(), Colors);
        }

        // ---------- invalid swaps ----------

        [Test]
        public void SwapWithoutAMatch_IsInvalid_AndLeavesTheBoardUnchanged()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BRG");
            Tile[] before = Snapshot(board);

            ResolveResult result = ResolverThatMustNotRefill().ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.TotalCleared, Is.EqualTo(0));
            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void NonAdjacentSwap_IsInvalid()
        {
            // Swapping (0,0) and (2,0) would be a legal swap if the tiles were neighbors, but they are not.
            Board board = TestBoards.FromRows("RGRGR");
            Tile[] before = Snapshot(board);

            ResolveResult result = ResolverThatMustNotRefill().ResolveSwap(board, new GridPos(0, 0), new GridPos(2, 0));

            Assert.That(result.IsValid, Is.False);
            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void DiagonalSwap_IsInvalid()
        {
            Board board = TestBoards.FromRows(
                "RB",
                "BR");
            Tile[] before = Snapshot(board);

            ResolveResult result = ResolverThatMustNotRefill().ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 1));

            Assert.That(result.IsValid, Is.False);
            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void SwapWithTheSameCell_IsInvalid()
        {
            Board board = TestBoards.FromRows("RRR");
            Tile[] before = Snapshot(board);

            ResolveResult result = ResolverThatMustNotRefill().ResolveSwap(board, new GridPos(1, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.False);
            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void SwapOutsideTheBoard_IsInvalid()
        {
            Board board = TestBoards.FromRows("RGB");
            Tile[] before = Snapshot(board);
            BoardResolver resolver = ResolverThatMustNotRefill();

            Assert.That(resolver.ResolveSwap(board, new GridPos(0, 0), new GridPos(-1, 0)).IsValid, Is.False);
            Assert.That(resolver.ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0)).IsValid, Is.False);
            Assert.That(resolver.ResolveSwap(board, new GridPos(0, 0), new GridPos(0, 1)).IsValid, Is.False);
            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void SwapWithAnEmptyCell_IsInvalid()
        {
            Board board = TestBoards.FromRows("RR.R");
            Tile[] before = Snapshot(board);

            ResolveResult result = ResolverThatMustNotRefill().ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            Assert.That(result.IsValid, Is.False);
            AssertBoardUnchanged(board, before);
        }

        // ---------- a hand-made cascade, step by step ----------

        // Column 0, bottom to top: R R G B G. Swapping (0,2) with the Red at (1,2) makes a vertical RRR.
        // After the Reds are cleared, B and G fall, and the B lands next to the two Bs on the bottom row: a second match.
        private static Board CascadeBoard()
        {
            return TestBoards.FromRows(
                "GYP",   // y = 4
                "BPY",   // y = 3
                "GRP",   // y = 2
                "RPY",   // y = 1
                "RBB");  // y = 0
        }

        [Test]
        public void Cascade_IsResolvedInOneCall_WithStepsInOrder()
        {
            Board board = CascadeBoard();
            // Refill script (indices into Colors):
            //   wave 1 fills column 0 at y=2,3,4  -> Yellow, Red, Green
            //   wave 2 fills y=4 in columns 0,1,2 -> Blue, Red, Green
            ScriptedRandom random = new ScriptedRandom(3, 0, 1, 2, 0, 1);
            BoardResolver resolver = new BoardResolver(random, Colors);
            Tile tileAtA = board.Get(0, 2);
            Tile tileAtB = board.Get(1, 2);

            ResolveResult result = resolver.ResolveSwap(board, new GridPos(0, 2), new GridPos(1, 2));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.WaveCount, Is.EqualTo(2));

            // swap, then wave 1: clear + fall + spawn, then wave 2: clear + fall + spawn
            Assert.That(result.Steps.Count, Is.EqualTo(7));
            Assert.That(result.Steps[0], Is.TypeOf<SwapStep>());
            Assert.That(result.Steps[1], Is.TypeOf<ClearStep>());
            Assert.That(result.Steps[2], Is.TypeOf<FallStep>());
            Assert.That(result.Steps[3], Is.TypeOf<SpawnStep>());
            Assert.That(result.Steps[4], Is.TypeOf<ClearStep>());
            Assert.That(result.Steps[5], Is.TypeOf<FallStep>());
            Assert.That(result.Steps[6], Is.TypeOf<SpawnStep>());

            Assert.That(result.Steps[0].Wave, Is.EqualTo(0));
            Assert.That(result.Steps[1].Wave, Is.EqualTo(1));
            Assert.That(result.Steps[3].Wave, Is.EqualTo(1));
            Assert.That(result.Steps[4].Wave, Is.EqualTo(2));
            Assert.That(result.Steps[6].Wave, Is.EqualTo(2));

            SwapStep swap = (SwapStep)result.Steps[0];
            Assert.That(swap.A, Is.EqualTo(new GridPos(0, 2)));
            Assert.That(swap.B, Is.EqualTo(new GridPos(1, 2)));
            Assert.That(swap.TileIdA, Is.EqualTo(tileAtA.Id)); // ids of the tiles that were there BEFORE the swap
            Assert.That(swap.TileIdB, Is.EqualTo(tileAtB.Id));

            Assert.That(((ClearStep)result.Steps[1]).Tiles.Count, Is.EqualTo(3)); // the three Reds
            Assert.That(((FallStep)result.Steps[2]).Moves.Count, Is.EqualTo(2));  // B and G in column 0
            Assert.That(((SpawnStep)result.Steps[3]).Spawns.Count, Is.EqualTo(3));
            Assert.That(((ClearStep)result.Steps[4]).Tiles.Count, Is.EqualTo(3)); // the three Blues
            Assert.That(((FallStep)result.Steps[5]).Moves.Count, Is.EqualTo(12)); // 4 tiles fall in each of 3 columns
            Assert.That(((SpawnStep)result.Steps[6]).Spawns.Count, Is.EqualTo(3));

            Assert.That(random.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void Cascade_CountsClearedTilesPerColor()
        {
            Board board = CascadeBoard();
            BoardResolver resolver = new BoardResolver(new ScriptedRandom(3, 0, 1, 2, 0, 1), Colors);

            ResolveResult result = resolver.ResolveSwap(board, new GridPos(0, 2), new GridPos(1, 2));

            Assert.That(result.GetClearedCount(TileColor.Red), Is.EqualTo(3));
            Assert.That(result.GetClearedCount(TileColor.Blue), Is.EqualTo(3));
            Assert.That(result.GetClearedCount(TileColor.Green), Is.EqualTo(0));
            Assert.That(result.TotalCleared, Is.EqualTo(6));
        }

        [Test]
        public void Cascade_LeavesTheExpectedFinalBoard()
        {
            Board board = CascadeBoard();
            Tile survivingGreen = board.Get(0, 4); // the G at the top of column 0 falls to y=1 in wave 1, then to y=0 in wave 2
            BoardResolver resolver = new BoardResolver(new ScriptedRandom(3, 0, 1, 2, 0, 1), Colors);

            resolver.ResolveSwap(board, new GridPos(0, 2), new GridPos(1, 2));

            AssertBoardFullAndStable(board);
            Assert.That(board.Get(0, 0), Is.SameAs(survivingGreen)); // same tile object, it only fell
            // Expected final board, top row first (y = 4 down to y = 0).
            string[] expected =
            {
                "BRG",
                "GYP",
                "RPY",
                "YGP",
                "GPY"
            };
            for (int row = 0; row < expected.Length; row++)
            {
                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    TileColor expectedColor = TileColorOf(expected[row][x]);
                    Assert.That(board.Get(x, y).Color, Is.EqualTo(expectedColor), "Cell (" + x + "," + y + ")");
                }
            }
        }

        private static TileColor TileColorOf(char letter)
        {
            switch (letter)
            {
                case 'R': return TileColor.Red;
                case 'G': return TileColor.Green;
                case 'B': return TileColor.Blue;
                case 'Y': return TileColor.Yellow;
                case 'P': return TileColor.Purple;
                default: throw new ArgumentException(letter.ToString());
            }
        }

        // ---------- a simple valid swap ----------

        [Test]
        public void SimpleMatch_ClearsThreeAndRefills()
        {
            // Swapping the Blue and the last Red makes R R R. The three new tiles are scripted to be
            // Green, Blue, Green so they cannot start a cascade.
            Board board = TestBoards.FromRows("RRBR");
            ScriptedRandom random = new ScriptedRandom(1, 2, 1);
            BoardResolver resolver = new BoardResolver(random, Colors);

            ResolveResult result = resolver.ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.WaveCount, Is.EqualTo(1));
            Assert.That(result.GetClearedCount(TileColor.Red), Is.EqualTo(3));
            Assert.That(board.Get(3, 0).Color, Is.EqualTo(TileColor.Blue)); // the Blue moved here and was not cleared
            Assert.That(board.Get(0, 0).Color, Is.EqualTo(TileColor.Green));
            Assert.That(board.Get(1, 0).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(board.Get(2, 0).Color, Is.EqualTo(TileColor.Green));
            Assert.That(random.Remaining, Is.EqualTo(0));
        }

        // ---------- properties that must hold for any seed ----------

        [Test]
        public void ManyRandomBoards_ResolveToAFullStableBoard()
        {
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            TileColor[] colors = new TileColor[5];
            Array.Copy(TestBoards.AllColors, colors, 5);

            for (int seed = 0; seed < 100; seed++)
            {
                Board board = new BoardGenerator(new SystemRandom(seed)).Generate(8, 8, colors);
                Assert.That(moveFinder.TryFindMove(board, out GridPos a, out GridPos b), Is.True);
                BoardResolver resolver = new BoardResolver(new SystemRandom(seed + 1000), colors);

                ResolveResult result = resolver.ResolveSwap(board, a, b);

                Assert.That(result.IsValid, Is.True, "Seed " + seed);
                Assert.That(result.TotalCleared, Is.GreaterThanOrEqualTo(3), "Seed " + seed);
                Assert.That(result.Steps[0], Is.TypeOf<SwapStep>(), "Seed " + seed);
                AssertBoardFullAndStable(board);
            }
        }

        [Test]
        public void EverySpawnStepRefillsExactlyTheCellsThatTheClearLeftEmpty()
        {
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            TileColor[] colors = new TileColor[4];
            Array.Copy(TestBoards.AllColors, colors, 4);

            for (int seed = 0; seed < 100; seed++)
            {
                Board board = new BoardGenerator(new SystemRandom(seed)).Generate(7, 9, colors);
                moveFinder.TryFindMove(board, out GridPos a, out GridPos b);
                ResolveResult result = new BoardResolver(new SystemRandom(seed), colors).ResolveSwap(board, a, b);

                // A wave clears some tiles; a match of 4 or an L/T shape puts one special tile back; the refill fills the rest.
                int clearedInWave = -1;
                int createdInWave = 0;
                for (int i = 0; i < result.Steps.Count; i++)
                {
                    if (result.Steps[i] is ClearStep clear)
                    {
                        clearedInWave = clear.Tiles.Count;
                        createdInWave = 0;
                    }
                    else if (result.Steps[i] is SpecialCreatedStep)
                    {
                        createdInWave++;
                    }
                    else if (result.Steps[i] is SpawnStep spawn)
                    {
                        Assert.That(spawn.Spawns.Count, Is.EqualTo(clearedInWave - createdInWave), "Seed " + seed + ", wave " + spawn.Wave);
                    }
                }
            }
        }

        [Test]
        public void SameSeed_ResolvesTheSameWay()
        {
            TileColor[] colors = new TileColor[5];
            Array.Copy(TestBoards.AllColors, colors, 5);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());

            Board first = new BoardGenerator(new SystemRandom(11)).Generate(8, 8, colors);
            Board second = new BoardGenerator(new SystemRandom(11)).Generate(8, 8, colors);
            moveFinder.TryFindMove(first, out GridPos a, out GridPos b);

            ResolveResult r1 = new BoardResolver(new SystemRandom(99), colors).ResolveSwap(first, a, b);
            ResolveResult r2 = new BoardResolver(new SystemRandom(99), colors).ResolveSwap(second, a, b);

            Assert.That(r2.Steps.Count, Is.EqualTo(r1.Steps.Count));
            Assert.That(r2.TotalCleared, Is.EqualTo(r1.TotalCleared));
        }

        [Test]
        public void ResolvingTwiceInARow_WorksWithOneResolver()
        {
            TileColor[] colors = new TileColor[5];
            Array.Copy(TestBoards.AllColors, colors, 5);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            Board board = new BoardGenerator(new SystemRandom(3)).Generate(8, 8, colors);
            BoardResolver resolver = new BoardResolver(new SystemRandom(4), colors);

            for (int turn = 0; turn < 20; turn++)
            {
                Assert.That(moveFinder.TryFindMove(board, out GridPos a, out GridPos b), Is.True, "Turn " + turn);
                Assert.That(resolver.ResolveSwap(board, a, b).IsValid, Is.True, "Turn " + turn);
                AssertBoardFullAndStable(board);
            }
        }
    }
}
