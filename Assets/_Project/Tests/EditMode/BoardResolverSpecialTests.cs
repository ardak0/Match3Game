using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>
    /// End-to-end tests of special tiles through BoardResolver. They look at the steps of wave 1 (the swap and the
    /// first clear), because those are fixed by the board the test builds. What the random refill does afterwards
    /// is not part of what these tests check.
    /// </summary>
    public class BoardResolverSpecialTests
    {
        private static BoardResolver NewResolver(int colorCount = 5, int seed = 1)
        {
            TileColor[] colors = new TileColor[colorCount];
            Array.Copy(TestBoards.AllColors, colors, colorCount);
            return new BoardResolver(new SystemRandom(seed), colors);
        }

        private static ClearStep FirstClear(ResolveResult result)
        {
            foreach (ResolveStep step in result.Steps)
            {
                if (step is ClearStep clear) return clear;
            }

            Assert.Fail("The result has no ClearStep.");
            return null;
        }

        private static HashSet<GridPos> CellsOf(ClearStep clear)
        {
            HashSet<GridPos> cells = new HashSet<GridPos>();
            foreach (ClearedTile tile in clear.Tiles)
            {
                cells.Add(tile.Position);
            }

            return cells;
        }

        private static HashSet<GridPos> Rectangle(int x0, int y0, int x1, int y1)
        {
            HashSet<GridPos> cells = new HashSet<GridPos>();
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    cells.Add(new GridPos(x, y));
                }
            }

            return cells;
        }

        private static SpecialCreatedStep FirstCreated(ResolveResult result)
        {
            foreach (ResolveStep step in result.Steps)
            {
                if (step is SpecialCreatedStep created) return created;
            }

            return null;
        }

        // ---------- creating specials ----------

        [Test]
        public void SwapMaking4InARow_LeavesAVerticalRocket_InTheSwappedCell()
        {
            // Swapping the Red at (2,1) down with the Blue at (2,0) makes R R R R along the bottom.
            Board board = TestBoards.FromRows(
                "GBRG",
                "RRBR");

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 0));

            Assert.That(result.IsValid, Is.True);
            Assert.That(FirstClear(result).Tiles.Count, Is.EqualTo(4));

            SpecialCreatedStep created = FirstCreated(result);
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Wave, Is.EqualTo(1));
            Assert.That(created.Special, Is.EqualTo(SpecialType.RocketVertical));
            Assert.That(created.Color, Is.EqualTo(TileColor.Red));
            Assert.That(created.Position, Is.EqualTo(new GridPos(2, 0)));
        }

        [Test]
        public void TheCreatedSpecialComesAfterTheClear_AndBeforeTheFall()
        {
            Board board = TestBoards.FromRows(
                "GBRG",
                "RRBR");

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 0));

            Assert.That(result.Steps[0], Is.TypeOf<SwapStep>());
            Assert.That(result.Steps[1], Is.TypeOf<ClearStep>());
            Assert.That(result.Steps[2], Is.TypeOf<SpecialCreatedStep>());
            Assert.That(result.Steps[3], Is.TypeOf<FallStep>());
        }

        [Test]
        public void TheCreatedSpecial_IsANewTile_WithAnIdNoOtherTileHad()
        {
            Board board = TestBoards.FromRows(
                "GBRG",
                "RRBR");
            HashSet<int> idsBefore = new HashSet<int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    idsBefore.Add(board.Get(x, y).Id);
                }
            }

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 0));

            Assert.That(idsBefore.Contains(FirstCreated(result).TileId), Is.False);
        }

        [Test]
        public void SwapMakingAVertical4_LeavesAHorizontalRocket()
        {
            // Column 0 reads R R B R from the bottom. Swapping the Blue with the Red beside it makes four Reds.
            Board board = TestBoards.FromRows(
                "RG",
                "BR",
                "RB",
                "RG");

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(0, 2), new GridPos(1, 2));

            Assert.That(FirstClear(result).Tiles.Count, Is.EqualTo(4));
            SpecialCreatedStep created = FirstCreated(result);
            Assert.That(created.Special, Is.EqualTo(SpecialType.RocketHorizontal));
            Assert.That(created.Position, Is.EqualTo(new GridPos(0, 2))); // cell A holds a Red after the swap, B holds the Blue
        }

        [Test]
        public void SwapMakingAnLShape_LeavesABomb()
        {
            // Swapping the Red at (1,0) up with the Blue at (1,1) makes Reds along row 1 (x 1..3) and column 1 (y 1..3).
            Board board = TestBoards.FromRows(
                "GRBG",
                "BRGB",
                "GBRR",
                "BRGP");

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(1, 0), new GridPos(1, 1));

            Assert.That(FirstClear(result).Tiles.Count, Is.EqualTo(5));
            SpecialCreatedStep created = FirstCreated(result);
            Assert.That(created.Special, Is.EqualTo(SpecialType.Bomb));
            Assert.That(created.Position, Is.EqualTo(new GridPos(1, 1)));
        }

        [Test]
        public void AnOrdinaryMatchOf3_CreatesNoSpecial()
        {
            Board board = TestBoards.FromRows("RRBR");

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            Assert.That(FirstCreated(result), Is.Null);
        }

        // ---------- setting specials off by swapping ----------

        [Test]
        public void SwappingARocket_IsAValidMove_AndItClearsItsRowAtTheNewPosition()
        {
            // No color match anywhere, but the rocket moves from (2,1) up to (2,2) and goes off there.
            Board board = TestBoards.Diagonal(5, 3, 5).WithSpecial(2, 1, SpecialType.RocketHorizontal);

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 2));

            Assert.That(result.IsValid, Is.True);
            ClearStep clear = FirstClear(result);
            Assert.That(CellsOf(clear), Is.EquivalentTo(Rectangle(0, 2, 4, 2)));
            Assert.That(clear.Tiles.Count, Is.EqualTo(5));
            Assert.That(result.TotalCleared, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void TheRocketThatWentOff_IsMarkedAsSpecialInTheClearStep_AtDepth0()
        {
            Board board = TestBoards.Diagonal(5, 3, 5).WithSpecial(2, 1, SpecialType.RocketHorizontal);

            ClearStep clear = FirstClear(NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 2)));

            int specialCount = 0;
            foreach (ClearedTile tile in clear.Tiles)
            {
                if (tile.Special != SpecialType.None)
                {
                    specialCount++;
                    Assert.That(tile.Special, Is.EqualTo(SpecialType.RocketHorizontal));
                    Assert.That(tile.ChainDepth, Is.EqualTo(0));
                    Assert.That(tile.Position, Is.EqualTo(new GridPos(2, 2)));
                }
                else
                {
                    Assert.That(tile.ChainDepth, Is.EqualTo(1));
                }
            }

            Assert.That(specialCount, Is.EqualTo(1));
        }

        [Test]
        public void SwappingAVerticalRocketSideways_ClearsTheColumnItLandsIn()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithSpecial(2, 2, SpecialType.RocketVertical);

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 2), new GridPos(3, 2));

            Assert.That(CellsOf(FirstClear(result)), Is.EquivalentTo(Rectangle(3, 0, 3, 4)));
        }

        [Test]
        public void ABombSwappedIntoACorner_ClearsOnlyTheCellsThatExist()
        {
            Board board = TestBoards.Diagonal(4, 4, 4).WithSpecial(1, 0, SpecialType.Bomb);

            ResolveResult result = NewResolver(4).ResolveSwap(board, new GridPos(1, 0), new GridPos(0, 0));

            Assert.That(CellsOf(FirstClear(result)), Is.EquivalentTo(Rectangle(0, 0, 1, 1)));
        }

        [Test]
        public void AChainReaction_ClearsEverythingAnyLinkOfTheChainHits()
        {
            // The bomb is swapped to (0,1). Its blast hits the vertical rocket at (1,2), whose column hits the
            // horizontal rocket at (1,4), whose row is row 4.
            Board board = TestBoards.Diagonal(5, 5, 5)
                .WithSpecial(1, 1, SpecialType.Bomb)
                .WithSpecial(1, 2, SpecialType.RocketVertical)
                .WithSpecial(1, 4, SpecialType.RocketHorizontal);

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(1, 1), new GridPos(0, 1));

            ClearStep clear = FirstClear(result);
            HashSet<GridPos> expected = Rectangle(0, 0, 1, 2); // the bomb's 3x3, clipped at the left edge
            expected.UnionWith(Rectangle(1, 0, 1, 4));         // the rocket's column
            expected.UnionWith(Rectangle(0, 4, 4, 4));         // the second rocket's row
            Assert.That(CellsOf(clear), Is.EquivalentTo(expected));
            Assert.That(clear.Tiles.Count, Is.EqualTo(expected.Count), "A tile must not be cleared twice.");

            int deepest = 0;
            foreach (ClearedTile tile in clear.Tiles)
            {
                deepest = Math.Max(deepest, tile.ChainDepth);
            }

            Assert.That(deepest, Is.EqualTo(3));
        }

        [Test]
        public void ClearedSpecialTiles_CountTowardTheirColor()
        {
            Board board = TestBoards.Diagonal(5, 3, 5).WithSpecial(2, 1, SpecialType.RocketHorizontal);

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 2));

            int fromSteps = 0;
            foreach (ResolveStep step in result.Steps)
            {
                if (step is ClearStep clear) fromSteps += clear.Tiles.Count;
            }

            Assert.That(result.TotalCleared, Is.EqualTo(fromSteps));
        }

        // ---------- combos ----------

        [Test]
        public void SwappingTwoBombs_ClearsA5x5SquareAroundTheCellSwipedTo()
        {
            Board board = TestBoards.Diagonal(6, 6, 6)
                .WithSpecial(2, 2, SpecialType.Bomb)
                .WithSpecial(3, 2, SpecialType.Bomb);

            ResolveResult result = NewResolver(6).ResolveSwap(board, new GridPos(2, 2), new GridPos(3, 2));

            Assert.That(CellsOf(FirstClear(result)), Is.EquivalentTo(Rectangle(1, 0, 5, 4)));
        }

        [Test]
        public void SwappingTwoRockets_ClearsItsRowAndColumn()
        {
            Board board = TestBoards.Diagonal(6, 6, 6)
                .WithSpecial(2, 2, SpecialType.RocketHorizontal)
                .WithSpecial(3, 2, SpecialType.RocketVertical);

            ResolveResult result = NewResolver(6).ResolveSwap(board, new GridPos(2, 2), new GridPos(3, 2));

            HashSet<GridPos> expected = Rectangle(0, 2, 5, 2);
            expected.UnionWith(Rectangle(3, 0, 3, 5));
            Assert.That(CellsOf(FirstClear(result)), Is.EquivalentTo(expected));
        }

        [Test]
        public void SwappingARocketWithABomb_Clears3RowsAnd3Columns()
        {
            Board board = TestBoards.Diagonal(6, 6, 6)
                .WithSpecial(2, 2, SpecialType.Bomb)
                .WithSpecial(3, 2, SpecialType.RocketHorizontal);

            ResolveResult result = NewResolver(6).ResolveSwap(board, new GridPos(2, 2), new GridPos(3, 2));

            HashSet<GridPos> expected = Rectangle(0, 1, 5, 3);
            expected.UnionWith(Rectangle(2, 0, 4, 5));
            Assert.That(CellsOf(FirstClear(result)), Is.EquivalentTo(expected));
        }

        [Test]
        public void ComboTilesAreClearedAtDepth0()
        {
            Board board = TestBoards.Diagonal(6, 6, 6)
                .WithSpecial(2, 2, SpecialType.Bomb)
                .WithSpecial(3, 2, SpecialType.Bomb);

            ClearStep clear = FirstClear(NewResolver(6).ResolveSwap(board, new GridPos(2, 2), new GridPos(3, 2)));

            foreach (ClearedTile tile in clear.Tiles)
            {
                bool isComboTile = tile.Position == new GridPos(2, 2) || tile.Position == new GridPos(3, 2);
                Assert.That(tile.ChainDepth, Is.EqualTo(isComboTile ? 0 : 1), "Cell " + tile.Position);
            }
        }

        // ---------- invalid swaps still leave the board alone ----------

        [Test]
        public void ASwapOfTwoOrdinaryTilesWithoutAMatch_IsStillInvalid_EvenWhenSpecialsAreOnTheBoard()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithSpecial(4, 4, SpecialType.Bomb);
            Tile before = board.Get(0, 0);

            ResolveResult result = NewResolver().ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.False);
            Assert.That(board.Get(0, 0), Is.SameAs(before));
        }

        // ---------- properties that must hold for any seed ----------

        [Test]
        public void ManyGamesWithSpecials_AlwaysEndOnAFullStableBoard_AndTheStepsAreConsistent()
        {
            TileColor[] colors = new TileColor[5];
            Array.Copy(TestBoards.AllColors, colors, 5);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            MatchFinder matchFinder = new MatchFinder();
            List<Match> matches = new List<Match>();
            SpecialType[] kinds = { SpecialType.RocketHorizontal, SpecialType.RocketVertical, SpecialType.Bomb };

            for (int seed = 0; seed < 150; seed++)
            {
                SystemRandom random = new SystemRandom(seed);
                Board board = new BoardGenerator(random).Generate(8, 8, colors);
                for (int i = 0; i < 4; i++)
                {
                    board.WithSpecial(random.Next(0, 8), random.Next(0, 8), kinds[random.Next(0, kinds.Length)]);
                }

                BoardResolver resolver = new BoardResolver(random, colors);
                HashSet<int> idsEverSeen = new HashSet<int>();
                CollectIds(board, idsEverSeen);

                for (int turn = 0; turn < 6; turn++)
                {
                    List<(GridPos, GridPos)> moves = ValidMoves(board, moveFinder);
                    if (moves.Count == 0) break;
                    (GridPos a, GridPos b) = moves[random.Next(0, moves.Count)];

                    ResolveResult result = resolver.ResolveSwap(board, a, b);

                    string where = "Seed " + seed + ", turn " + turn;
                    Assert.That(result.IsValid, Is.True, where);
                    AssertStepsAreConsistent(result, idsEverSeen, where);

                    for (int y = 0; y < board.Height; y++)
                    {
                        for (int x = 0; x < board.Width; x++)
                        {
                            Assert.That(board.Get(x, y), Is.Not.Null, where + ": empty cell (" + x + "," + y + ")");
                        }
                    }

                    matchFinder.FindMatches(board, matches);
                    Assert.That(matches, Is.Empty, where + ": a match was left on the board");
                }
            }
        }

        private static void CollectIds(Board board, HashSet<int> ids)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ids.Add(board.Get(x, y).Id);
                }
            }
        }

        private static List<(GridPos, GridPos)> ValidMoves(Board board, MoveFinder moveFinder)
        {
            List<(GridPos, GridPos)> moves = new List<(GridPos, GridPos)>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    GridPos here = new GridPos(x, y);
                    if (x + 1 < board.Width && moveFinder.IsValidMove(board, here, new GridPos(x + 1, y))) moves.Add((here, new GridPos(x + 1, y)));
                    if (y + 1 < board.Height && moveFinder.IsValidMove(board, here, new GridPos(x, y + 1))) moves.Add((here, new GridPos(x, y + 1)));
                }
            }

            return moves;
        }

        // Every cleared tile existed and is cleared once, every new tile has a brand new id,
        // and each wave refills exactly the cells that are still empty after the clear and the created specials.
        private static void AssertStepsAreConsistent(ResolveResult result, HashSet<int> idsEverSeen, string where)
        {
            HashSet<int> cleared = new HashSet<int>();
            int clearedInWave = 0;
            int createdInWave = 0;
            int totalCleared = 0;

            foreach (ResolveStep step in result.Steps)
            {
                if (step is ClearStep clear)
                {
                    clearedInWave = clear.Tiles.Count;
                    createdInWave = 0;
                    totalCleared += clear.Tiles.Count;
                    foreach (ClearedTile tile in clear.Tiles)
                    {
                        Assert.That(idsEverSeen.Contains(tile.TileId), Is.True, where + ": cleared a tile that never existed");
                        Assert.That(cleared.Add(tile.TileId), Is.True, where + ": a tile was cleared twice");
                    }
                }
                else if (step is SpecialCreatedStep created)
                {
                    createdInWave++;
                    Assert.That(idsEverSeen.Add(created.TileId), Is.True, where + ": a created special reused an id");
                }
                else if (step is SpawnStep spawn)
                {
                    Assert.That(spawn.Spawns.Count, Is.EqualTo(clearedInWave - createdInWave), where + ": wrong refill size");
                    foreach (TileSpawn tileSpawn in spawn.Spawns)
                    {
                        Assert.That(idsEverSeen.Add(tileSpawn.TileId), Is.True, where + ": a spawned tile reused an id");
                    }
                }
            }

            Assert.That(result.TotalCleared, Is.EqualTo(totalCleared), where);
        }
    }
}
