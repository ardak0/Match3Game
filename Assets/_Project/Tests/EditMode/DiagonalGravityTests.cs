using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    /// <summary>
    /// Gravity and refill when crates and chained tiles are in the way.
    /// A cell under a blocker cannot be filled from straight above, so it takes the tile from its upper-left cell,
    /// or else from its upper-right cell. Tiles never slide into a cell that has an empty cell above it.
    /// </summary>
    public class DiagonalGravityTests
    {
        // A 3x4 board with a crate at (1,1) and an empty cell (1,0) under it. Every other cell holds a tile.
        private static Board CrateWithHoleBelow()
        {
            return TestBoards.FromRows(
                "RGB",
                "GBR",
                "B.G",
                "R.B").WithObstacles(
                "...",
                "...",
                ".C.",
                "...");
        }

        // ---------- straight gravity next to blockers ----------

        [Test]
        public void StraightFall_NeverPassesACrate()
        {
            Board board = CrateWithHoleBelow();
            Tile above = board.Get(1, 2);

            FallStep step = new GravityResolver().Apply(board, 1);

            Assert.That(step, Is.Null, "Nothing can fall: the only gap is under the crate.");
            Assert.That(board.Get(1, 2), Is.SameAs(above));
            Assert.That(board.Get(1, 0), Is.Null);
            Assert.That(board.HasCrate(1, 1), Is.True);
        }

        [Test]
        public void StraightFall_StillWorksAboveACrate()
        {
            // The gap at (1,2) is above the crate, so the tile at (1,3) simply falls onto it. The gap under the crate stays empty.
            Board board = TestBoards.FromRows(
                "RGB",
                "B.G",
                "G.R",
                "R.B").WithObstacles(
                "...",
                "...",
                ".C.",
                "...");
            Tile top = board.Get(1, 3);

            FallStep step = new GravityResolver().Apply(board, 1);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Moves.Count, Is.EqualTo(1));
            Assert.That(step.Moves[0].TileId, Is.EqualTo(top.Id));
            Assert.That(step.Moves[0].To, Is.EqualTo(new GridPos(1, 2)));
            Assert.That(board.Get(1, 0), Is.Null);
        }

        [Test]
        public void StraightFall_StopsOnAChainedTile()
        {
            // The chained tile at (1,1) stays where it is, and the tile above it cannot pass it.
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "BYG",
                "R.B").WithObstacles(
                "...",
                "...",
                ".L.",
                "...");
            Tile chained = board.Get(1, 1);

            FallStep step = new GravityResolver().Apply(board, 1);

            Assert.That(step, Is.Null);
            Assert.That(board.Get(1, 1), Is.SameAs(chained));
            Assert.That(board.Get(1, 0), Is.Null, "The chained tile does not fall into the gap below it.");
        }

        // ---------- the diagonal pass ----------

        [Test]
        public void Diagonal_FillsTheCellUnderACrate_FromTheUpperLeftFirst()
        {
            Board board = CrateWithHoleBelow();
            Tile upperLeft = board.Get(0, 1);
            Tile upperRight = board.Get(2, 1);

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 5);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Moves.Count, Is.EqualTo(1));
            Assert.That(step.Moves[0].TileId, Is.EqualTo(upperLeft.Id));
            Assert.That(step.Moves[0].From, Is.EqualTo(new GridPos(0, 1)));
            Assert.That(step.Moves[0].To, Is.EqualTo(new GridPos(1, 0)));
            Assert.That(step.Round, Is.EqualTo(5));
            Assert.That(step.Wave, Is.EqualTo(1));
            Assert.That(board.Get(1, 0), Is.SameAs(upperLeft));
            Assert.That(board.Get(0, 1), Is.Null);
            Assert.That(board.Get(2, 1), Is.SameAs(upperRight), "The right neighbor is only used when the left one cannot help.");
        }

        [Test]
        public void Diagonal_TakesTheUpperRightTile_WhenTheUpperLeftCellHasNoTile()
        {
            Board board = CrateWithHoleBelow();
            board.Set(0, 1, null);
            Tile upperRight = board.Get(2, 1);

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 1);

            Assert.That(step.Moves[0].From, Is.EqualTo(new GridPos(2, 1)));
            Assert.That(board.Get(1, 0), Is.SameAs(upperRight));
        }

        [Test]
        public void Diagonal_TakesTheUpperRightTile_WhenTheUpperLeftTileIsChained()
        {
            Board board = CrateWithHoleBelow();
            board.SetObstacle(0, 1, new Obstacle(ObstacleType.Chain, 1));
            Tile chained = board.Get(0, 1);
            Tile upperRight = board.Get(2, 1);

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 1);

            Assert.That(step.Moves[0].From, Is.EqualTo(new GridPos(2, 1)));
            Assert.That(board.Get(1, 0), Is.SameAs(upperRight));
            Assert.That(board.Get(0, 1), Is.SameAs(chained), "A chained tile never moves, not even sideways.");
        }

        [Test]
        public void Diagonal_ReturnsNull_WhenBothUpperCellsCannotHelp()
        {
            // (1,1) has a crate above it and crates on both upper diagonals, so nothing can reach it.
            Board board = TestBoards.FromRows(
                "...",
                "R.B",
                "GRG").WithObstacles(
                "CCC",
                "...",
                "...");

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 1);

            Assert.That(step, Is.Null);
            Assert.That(board.Get(1, 1), Is.Null);
        }

        [Test]
        public void Diagonal_FillsOnlyUnderTheBlocker_NotTheCellUnderThatGap()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "G.B",
                "B.G",
                "R.B").WithObstacles(
                "...",
                ".C.",
                "...",
                "...");
            Tile upperLeft = board.Get(0, 2);

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 1);

            Assert.That(step.Moves.Count, Is.EqualTo(1), "Only the cell directly under the crate is filled in this pass.");
            Assert.That(step.Moves[0].To, Is.EqualTo(new GridPos(1, 1)));
            Assert.That(board.Get(1, 1), Is.SameAs(upperLeft));
            Assert.That(board.Get(1, 0), Is.Null);
        }

        [Test]
        public void Diagonal_CellUnderAChainedTile_IsFilledToo()
        {
            Board board = CrateWithHoleBelow();
            board.SetObstacle(1, 1, Obstacle.None);
            board.Set(1, 1, board.NewTile(TileColor.Yellow));
            board.SetObstacle(1, 1, new Obstacle(ObstacleType.Chain, 1));
            Tile upperLeft = board.Get(0, 1);

            FallStep step = new GravityResolver().ApplyDiagonal(board, 1, 1);

            Assert.That(step, Is.Not.Null);
            Assert.That(board.Get(1, 0), Is.SameAs(upperLeft));
        }

        [Test]
        public void Diagonal_WithoutAnyBlocker_DoesNothing()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "GBR",
                "B.G",
                "R.B");

            Assert.That(new GravityResolver().ApplyDiagonal(board, 1, 1), Is.Null, "Plain gaps are for the straight pass and the refill.");
        }

        // ---------- refill ----------

        [Test]
        public void Refill_DoesNotSpawnIntoACellUnderACrate()
        {
            Board board = TestBoards.FromRows(
                "R.B",
                "G.R",
                "B.G",
                "R.B").WithObstacles(
                "...",
                ".C.",
                "...",
                "...");

            SpawnStep step = new Refiller(new ScriptedRandom(2, 2, 2), Colors).Refill(board, 1);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Spawns.Count, Is.EqualTo(1), "Only the cell above the crate can receive a new tile from the top.");
            Assert.That(step.Spawns[0].To, Is.EqualTo(new GridPos(1, 3)));
            Assert.That(board.Get(1, 0), Is.Null);
            Assert.That(board.Get(1, 1), Is.Null, "A crate cell stays without a tile.");
        }

        [Test]
        public void Refill_FillsEveryColumnThatIsNotBlocked()
        {
            Board board = new Board(3, 3);
            board.SetObstacle(1, 2, new Obstacle(ObstacleType.Crate, 1)); // the top of column 1 is a crate

            SpawnStep step = new Refiller(new SystemRandom(1), Colors).Refill(board, 1);

            Assert.That(step.Spawns.Count, Is.EqualTo(6), "Columns 0 and 2 get 3 tiles each; column 1 is blocked at the top and gets none.");
            Assert.That(board.Get(1, 0), Is.Null);
            Assert.That(board.Get(1, 1), Is.Null);
            Assert.That(board.Get(0, 2), Is.Not.Null);
        }

        // ---------- the whole resolve ----------

        // A 5x4 board with a 2 HP crate at (1,1) that survives the match, so the cell under it has to be filled from the side.
        private static Board SlideBoard()
        {
            return TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "B.PRG",
                "RRGRP").WithObstacles(
                ".....",
                ".....",
                ".D...",
                ".....");
        }

        [Test]
        public void Resolve_FillsTheCellUnderACrate_ByASlide_AndLeavesNoHole()
        {
            Board board = SlideBoard();

            ResolveResult result = ResolverWithRefill(3, 3, 4).ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            Assert.That(result.IsValid, Is.True);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.HasCrate(x, y)) continue;
                    Assert.That(board.Get(x, y), Is.Not.Null, "Hole at (" + x + "," + y + ")\n" + Describe(board));
                }
            }

            bool slid = false;
            foreach (FallStep fall in AllStepsOf<FallStep>(result))
            {
                foreach (TileMove move in fall.Moves)
                {
                    if (move.From.X != move.To.X) slid = true;
                }
            }

            Assert.That(slid, Is.True, "The cell under the crate can only be filled by a diagonal move.");
        }

        [Test]
        public void Resolve_PlaysSlidesAfterTheFallsTheyWaitFor_InIncreasingRounds()
        {
            Board board = SlideBoard();

            ResolveResult result = ResolverWithRefill(3, 3, 4).ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            // Within one wave the rounds never go back. A slide is in an odd round, straight falls and spawns in even ones.
            int lastWave = -1;
            int lastRound = -1;
            foreach (ResolveStep step in result.Steps)
            {
                int round;
                if (step is FallStep fall)
                {
                    round = fall.Round;
                    bool diagonal = false;
                    foreach (TileMove move in fall.Moves) diagonal |= move.From.X != move.To.X;
                    Assert.That(diagonal, Is.EqualTo(round % 2 == 1), "Slides belong to odd rounds and straight falls to even ones.");
                }
                else if (step is SpawnStep spawn)
                {
                    round = spawn.Round;
                }
                else
                {
                    continue;
                }

                if (step.Wave != lastWave)
                {
                    lastWave = step.Wave;
                    lastRound = -1;
                }

                Assert.That(round, Is.GreaterThanOrEqualTo(lastRound), "Rounds went backwards in wave " + step.Wave + ".");
                lastRound = round;
            }
        }

        [Test]
        public void Resolve_IsDeterministic_ForTheSameSeed()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                Board first = SlideBoard();
                Board second = SlideBoard();

                ResolveResult a = new BoardResolver(new SystemRandom(seed), Colors).ResolveSwap(first, new GridPos(2, 0), new GridPos(3, 0));
                ResolveResult b = new BoardResolver(new SystemRandom(seed), Colors).ResolveSwap(second, new GridPos(2, 0), new GridPos(3, 0));

                Assert.That(Signature(a), Is.EqualTo(Signature(b)), "seed " + seed);
                Assert.That(Describe(first), Is.EqualTo(Describe(second)), "seed " + seed);
            }
        }

        [Test]
        public void Resolve_WithObstaclesEverywhere_NeverLeavesAHoleOrAMatch()
        {
            // A wider board with crates, ice and chains, resolved with many seeds: it must always end full, stable and match-free.
            for (int seed = 0; seed < 40; seed++)
            {
                ObstacleLayout.TryParse(new[]
                {
                    ".C...C.",
                    "..J.L..",
                    ".C.D...",
                    "...I...",
                    "L.....C",
                    "......."
                }, 7, 6, out ObstacleLayout layout, out _);

                SystemRandom random = new SystemRandom(seed);
                Board board = new BoardGenerator(random).Generate(7, 6, Colors, layout);
                BoardResolver resolver = new BoardResolver(random, Colors);
                MoveFinder moveFinder = new MoveFinder(new MatchFinder());

                for (int move = 0; move < 10; move++)
                {
                    if (!moveFinder.TryFindMove(board, out GridPos a, out GridPos b)) break;

                    resolver.ResolveSwap(board, a, b);

                    List<Match> matches = new List<Match>();
                    new MatchFinder().FindMatches(board, matches);
                    Assert.That(matches, Is.Empty, "seed " + seed + " move " + move + "\n" + Describe(board));
                    for (int y = 0; y < board.Height; y++)
                    {
                        for (int x = 0; x < board.Width; x++)
                        {
                            bool isHole = board.Get(x, y) == null && !board.HasCrate(x, y);
                            bool shadowed = board.HasBlockerAbove(x, y);
                            Assert.That(isHole && !shadowed, Is.False, "seed " + seed + ": gap at (" + x + "," + y + ") that nothing can fill\n" + Describe(board));
                        }
                    }
                }
            }
        }

        [Test]
        public void Resolve_PutsTheObstacleStepsBetweenTheClearAndTheFall()
        {
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "B.PRG",
                "RRGRP").WithObstacles(
                ".....",
                ".....",
                ".C...",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            int clear = -1, destroyed = -1, fall = -1;
            for (int i = 0; i < result.Steps.Count; i++)
            {
                if (result.Steps[i] is ClearStep && clear < 0) clear = i;
                if (result.Steps[i] is ObstacleDestroyedStep && destroyed < 0) destroyed = i;
                if (result.Steps[i] is FallStep && fall < 0) fall = i;
            }

            Assert.That(clear, Is.GreaterThanOrEqualTo(0));
            Assert.That(destroyed, Is.GreaterThan(clear));
            Assert.That(fall, Is.GreaterThan(destroyed));
        }
    }
}
