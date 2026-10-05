using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    /// <summary>
    /// Everything about the ColorBomb: how it is created, why it never matches, what it clears alone and in combos,
    /// and what happens when another special's blast catches it.
    /// The first half tests the rules (MatchFinder, SpecialResolver); the second half runs whole swaps through BoardResolver.
    /// </summary>
    public class ColorBombTests
    {
        private static readonly GridPos NoSwap = new GridPos(-1, -1);

        // After this many scripted numbers the random falls back to a seeded one, so the refill after a clear still works.
        private sealed class ScriptedThenSeededRandom : IRandom
        {
            private readonly int[] _script;
            private readonly SystemRandom _fallback = new SystemRandom(1);
            private int _next;

            public ScriptedThenSeededRandom(params int[] script)
            {
                _script = script;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (_next < _script.Length) return _script[_next++];
                return _fallback.Next(minInclusive, maxExclusive);
            }
        }

        private static List<Match> FindMatches(Board board)
        {
            List<Match> matches = new List<Match>();
            new MatchFinder().FindMatches(board, matches);
            return matches;
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

        private static HashSet<GridPos> CellsOf(ClearSet set)
        {
            HashSet<GridPos> cells = new HashSet<GridPos>();
            for (int i = 0; i < set.Count; i++)
            {
                cells.Add(set.PositionAt(i));
            }

            return cells;
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

        private static HashSet<GridPos> CellsOfColor(Board board, TileColor color)
        {
            HashSet<GridPos> cells = new HashSet<GridPos>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.Get(x, y).Color == color) cells.Add(new GridPos(x, y));
                }
            }

            return cells;
        }

        private static T FindStep<T>(ResolveResult result) where T : ResolveStep
        {
            foreach (ResolveStep step in result.Steps)
            {
                if (step is T found) return found;
            }

            return null;
        }

        private static BoardResolver NewResolver(IRandom random, int colorCount = 5)
        {
            TileColor[] colors = new TileColor[colorCount];
            Array.Copy(TestBoards.AllColors, colors, colorCount);
            return new BoardResolver(random, colors);
        }

        // ---------- a ColorBomb never takes part in a normal match ----------

        [Test]
        public void ThreeColorBombsInARow_AreNotAMatch()
        {
            Assert.That(FindMatches(TestBoards.FromRows("***RG")), Is.Empty);
        }

        [Test]
        public void ThreeColorBombsInAColumn_AreNotAMatch()
        {
            Assert.That(FindMatches(TestBoards.FromRows("*R", "*G", "*B")), Is.Empty);
        }

        [Test]
        public void AColorBomb_DoesNotJoinARunOfItsNeighbors()
        {
            // Without the ColorBomb the two pairs would be a run of 4 only if it were red. It is not.
            Assert.That(FindMatches(TestBoards.FromRows("RR*RR")), Is.Empty);
        }

        [Test]
        public void AColorBomb_DoesNotStopARunNextToIt()
        {
            List<Match> matches = FindMatches(TestBoards.FromRows("*RRRG"));

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Positions.Count, Is.EqualTo(3));
            Assert.That(matches[0].Positions.Contains(new GridPos(0, 0)), Is.False);
        }

        // ---------- creation ----------

        private static SpecialCreation CreationOf(string[] rows)
        {
            Board board = TestBoards.FromRows(rows);
            List<Match> matches = FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1), "The test board should hold exactly one match.");

            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            Assert.That(resolver.TryGetCreation(matches[0], false, NoSwap, NoSwap, out SpecialCreation creation), Is.True);
            return creation;
        }

        [Test]
        public void HorizontalRunOf5_CreatesAColorBomb_NotARocket()
        {
            SpecialCreation creation = CreationOf(new[] { "GBGBG", "RRRRR" });

            Assert.That(creation.Special, Is.EqualTo(SpecialType.ColorBomb));
            Assert.That(creation.Color, Is.EqualTo(TileColor.None));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(2, 0)));
        }

        [Test]
        public void VerticalRunOf5_CreatesAColorBomb()
        {
            SpecialCreation creation = CreationOf(new[] { "RG", "RB", "RG", "RB", "RG" });

            Assert.That(creation.Special, Is.EqualTo(SpecialType.ColorBomb));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(0, 2)));
        }

        [Test]
        public void RunOf6_CreatesAColorBomb()
        {
            Assert.That(CreationOf(new[] { "GBGBGB", "RRRRRR" }).Special, Is.EqualTo(SpecialType.ColorBomb));
        }

        [Test]
        public void RunOf5_CrossingARunOf3_StillCreatesAColorBomb_NotABomb()
        {
            // The vertical run of 3 stands on the middle of the run of 5: a + shape. The run of 5 wins.
            SpecialCreation creation = CreationOf(new[]
            {
                "GBRBG",
                "BGRGB",
                "RRRRR"
            });

            Assert.That(creation.Special, Is.EqualTo(SpecialType.ColorBomb));
        }

        [Test]
        public void RunOf4_StillCreatesARocket_AndAnLShapeStillCreatesABomb()
        {
            Assert.That(CreationOf(new[] { "GBGB", "RRRR" }).Special, Is.EqualTo(SpecialType.RocketVertical));
            Assert.That(CreationOf(new[] { "RGB", "RBG", "RRR" }).Special, Is.EqualTo(SpecialType.Bomb));
        }

        [Test]
        public void TheColorBomb_AppearsInTheSwappedCell_WhenThatCellIsInTheRun()
        {
            Board board = TestBoards.FromRows("GBGBG", "RRRRR");
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            GridPos swapB = new GridPos(1, 0);

            resolver.TryGetCreation(FindMatches(board)[0], true, new GridPos(1, 1), swapB, out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(swapB));
        }

        // ---------- swapping a ColorBomb is always a valid move ----------

        [Test]
        public void SwappingAColorBombWithAnyTile_IsAValidMove_EvenWithoutAColorMatch()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(2, 2);
            MoveFinder finder = new MoveFinder(new MatchFinder());

            Assert.That(finder.WouldMatchAfterSwap(board, new GridPos(2, 2), new GridPos(3, 2)), Is.False);
            Assert.That(finder.IsValidMove(board, new GridPos(2, 2), new GridPos(3, 2)), Is.True);
            Assert.That(finder.IsValidMove(board, new GridPos(1, 2), new GridPos(2, 2)), Is.True);
            Assert.That(finder.IsValidMove(board, new GridPos(2, 2), new GridPos(2, 3)), Is.True);
        }

        // ---------- the rules, tested on the SpecialResolver ----------

        [Test]
        public void ColorBombPlusANormalTile_MarksEveryTileOfThatColor()
        {
            // Diagonal(5,5,5): the color of (x, y) is (x + y) % 5. The tile next to the bomb is green (color 1).
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            HashSet<GridPos> expected = CellsOfColor(board, TileColor.Green);
            Assert.That(expected.Count, Is.EqualTo(5));
            expected.Add(new GridPos(0, 0)); // the bomb itself is used up
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
            Assert.That(set.IsActivated(new GridPos(0, 0)), Is.True);
            Assert.That(set.DepthAt(new GridPos(0, 0)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(4, 2)), Is.EqualTo(1));
            Assert.That(resolver.Conversions, Is.Empty);
        }

        [Test]
        public void ItDoesNotMatterWhichOfTheTwoCellsHoldsTheColorBomb()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(1, 0);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0)); // the tile at A is red (color 0) now

            HashSet<GridPos> expected = CellsOfColor(board, TileColor.Red);
            expected.Add(new GridPos(1, 0));
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
        }

        [Test]
        public void TheFire_RecordsTheBombAndEveryTargetForTheView()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0);
            int bombId = board.Get(0, 0).Id;
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(resolver.ColorBombFires.Count, Is.EqualTo(1));
            ColorBombFire fire = resolver.ColorBombFires[0];
            Assert.That(fire.TileId, Is.EqualTo(bombId));
            Assert.That(fire.Position, Is.EqualTo(new GridPos(0, 0)));
            Assert.That(fire.Depth, Is.EqualTo(0));
            Assert.That(fire.Targets, Is.EquivalentTo(CellsOfColor(board, TileColor.Green)));
        }

        [Test]
        public void ColorBombPlusRocket_ConvertsEveryOtherPlainTileOfThatColor_WithRandomDirections()
        {
            // Green tiles: (1,0) is the swapped rocket, then (0,1) (4,2) (3,3) (2,4) in that scan order (row by row, bottom first).
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithSpecial(1, 0, SpecialType.RocketHorizontal);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom(0, 1, 1, 0)); // 0 = horizontal, 1 = vertical
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(resolver.Conversions.Count, Is.EqualTo(4));
            AssertConversion(resolver.Conversions[0], 0, 1, SpecialType.RocketHorizontal);
            AssertConversion(resolver.Conversions[1], 4, 2, SpecialType.RocketVertical);
            AssertConversion(resolver.Conversions[2], 3, 3, SpecialType.RocketVertical);
            AssertConversion(resolver.Conversions[3], 2, 4, SpecialType.RocketHorizontal);
            Assert.That(set.IsActivated(new GridPos(1, 0)), Is.False, "The swapped rocket is not used up: it fires like the others.");
        }

        private static void AssertConversion(SpecialConversion conversion, int x, int y, SpecialType expected)
        {
            Assert.That(conversion.Position, Is.EqualTo(new GridPos(x, y)));
            Assert.That(conversion.Special, Is.EqualTo(expected));
        }

        [Test]
        public void ColorBombPlusBomb_ConvertsThemToBombs_AndNeedsNoRandomness()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithSpecial(1, 0, SpecialType.Bomb);
            ScriptedRandom random = new ScriptedRandom(); // throws if the resolver asks for a number
            SpecialResolver resolver = new SpecialResolver(random);
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(resolver.Conversions.Count, Is.EqualTo(4));
            foreach (SpecialConversion conversion in resolver.Conversions)
            {
                Assert.That(conversion.Special, Is.EqualTo(SpecialType.Bomb));
            }
        }

        [Test]
        public void ColorBombPlusRocket_DoesNotDowngradeABombOfThatColor()
        {
            // The green tile at (4,2) is already a bomb. It is not turned into a rocket; it just goes off.
            Board board = TestBoards.Diagonal(5, 5, 5)
                .WithColorBomb(0, 0)
                .WithSpecial(1, 0, SpecialType.RocketHorizontal)
                .WithSpecial(4, 2, SpecialType.Bomb);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom(0, 0, 0));
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(resolver.Conversions.Count, Is.EqualTo(3));
            foreach (SpecialConversion conversion in resolver.Conversions)
            {
                Assert.That(conversion.Position, Is.Not.EqualTo(new GridPos(4, 2)));
            }

            Assert.That(set.IsMarked(new GridPos(4, 2)), Is.True);
        }

        [Test]
        public void ColorBombPlusColorBomb_MarksTheWholeBoard()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithColorBomb(1, 0);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);

            resolver.MarkColorBombSwap(board, set, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(0, 0, 4, 4)));
            Assert.That(set.DepthAt(new GridPos(0, 0)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(1, 0)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(4, 4)), Is.EqualTo(1));
        }

        // A 4x3 board with a clear winner: blue 5, red 4, green 3. Rows are written top first.
        private static Board WinnerBoard()
        {
            return TestBoards.FromRows(
                "BBRG",   // y = 2
                "RBGB",   // y = 1
                "GRBR");  // y = 0
        }

        [Test]
        public void ABlast_ThatHitsAColorBomb_ClearsTheMostCommonColorOnTheBoard()
        {
            // The bomb at (3,0) hits (2,0) (3,0) (2,1) (3,1). The ColorBomb at (2,1) is one of them.
            Board board = WinnerBoard().WithSpecial(3, 0, SpecialType.Bomb).WithColorBomb(2, 1);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);
            set.Mark(new GridPos(3, 0), 0);

            resolver.ActivateSpecials(board, set);

            HashSet<GridPos> expected = Rectangle(2, 0, 3, 1);
            expected.UnionWith(CellsOfColor(board, TileColor.Blue));
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
            Assert.That(set.DepthAt(new GridPos(2, 1)), Is.EqualTo(1), "Hit by the bomb.");
            Assert.That(set.DepthAt(new GridPos(0, 2)), Is.EqualTo(2), "Hit by the ColorBomb.");
            Assert.That(resolver.ColorBombFires.Count, Is.EqualTo(1));
            Assert.That(resolver.ColorBombFires[0].Depth, Is.EqualTo(1));
            Assert.That(resolver.ColorBombFires[0].Position, Is.EqualTo(new GridPos(2, 1)));
        }

        [Test]
        public void WhenColorsTie_TheLowestColorWins()
        {
            // Diagonal(5,5,5) has five tiles of every color. The ColorBomb replaces one red, so red has 4 and
            // green, blue, yellow and purple tie with 5: green is first in the enum and is the one cleared.
            Board board = TestBoards.Diagonal(5, 5, 5).WithSpecial(2, 2, SpecialType.Bomb).WithColorBomb(2, 3);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);
            set.Mark(new GridPos(2, 2), 0);

            resolver.ActivateSpecials(board, set);

            foreach (GridPos green in CellsOfColor(board, TileColor.Green))
            {
                Assert.That(set.IsMarked(green), Is.True, "Green tile at " + green + " should be cleared.");
            }

            Assert.That(set.IsMarked(new GridPos(0, 4)), Is.False, "(0,4) is purple and must stay.");
        }

        [Test]
        public void AColorBombCaughtInABlast_SetsOffTheSpecialsItClears_AChainReaction()
        {
            // The blue tile at (1,2) is a vertical rocket. When the ColorBomb clears blue, it goes off and clears column 1.
            Board board = WinnerBoard()
                .WithSpecial(3, 0, SpecialType.Bomb)
                .WithColorBomb(2, 1)
                .WithSpecial(1, 2, SpecialType.RocketVertical);
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);
            set.Mark(new GridPos(3, 0), 0);

            resolver.ActivateSpecials(board, set);

            Assert.That(set.IsMarked(new GridPos(1, 0)), Is.True, "Column 1 is cleared by the rocket the ColorBomb set off.");
            Assert.That(set.DepthAt(new GridPos(1, 0)), Is.EqualTo(3));
        }

        [Test]
        public void AColorBombCaughtInABlast_OnABoardWithNoColoredTiles_ClearsNothingExtra()
        {
            Board board = TestBoards.FromRows("**", "**");
            SpecialResolver resolver = new SpecialResolver(new ScriptedRandom());
            ClearSet set = new ClearSet();
            set.Reset(board);
            set.Mark(new GridPos(0, 0), 0);

            resolver.ActivateSpecials(board, set);

            Assert.That(set.Count, Is.EqualTo(1));
        }

        // ---------- whole swaps through BoardResolver ----------

        [Test]
        public void SwapMaking5InARow_LeavesAColorBomb_InTheSwappedCell_AndItHasNoColor()
        {
            // Swapping the red at (2,1) down with the blue at (2,0) makes R R R R R along the bottom.
            Board board = TestBoards.FromRows(
                "GBRYG",
                "RRBRR");

            ResolveResult result = NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(2, 1), new GridPos(2, 0));

            Assert.That(result.IsValid, Is.True);
            SpecialCreatedStep created = FindStep<SpecialCreatedStep>(result);
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Special, Is.EqualTo(SpecialType.ColorBomb));
            Assert.That(created.Color, Is.EqualTo(TileColor.None));
            Assert.That(created.Position, Is.EqualTo(new GridPos(2, 0)));
            Assert.That(board.Get(2, 0).Special, Is.EqualTo(SpecialType.ColorBomb));
            Assert.That(board.Get(2, 0).Id, Is.EqualTo(created.TileId));
        }

        [Test]
        public void SwappingColorBombWithANormalTile_ClearsAllOfThatColor()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0);
            HashSet<GridPos> greens = CellsOfColor(board, TileColor.Green); // (1,0) (0,1) (4,2) (3,3) (2,4)

            ResolveResult result = NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.True);
            // After the swap the green tile is at (0,0) and the bomb at (1,0).
            HashSet<GridPos> expected = new HashSet<GridPos>(greens) { new GridPos(0, 0), new GridPos(1, 0) };
            Assert.That(CellsOf(FindStep<ClearStep>(result)), Is.EquivalentTo(expected));
        }

        [Test]
        public void SwappingAColorBombIsValid_InEitherDirection()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(2, 2);

            Assert.That(NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(3, 2), new GridPos(2, 2)).IsValid, Is.True);
        }

        [Test]
        public void ColorBombPlusRocket_TurnsThemIntoRockets_ThenFiresThemAll()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithSpecial(1, 0, SpecialType.RocketHorizontal);
            int idOfGreenAt01 = board.Get(0, 1).Id;
            BoardResolver resolver = NewResolver(new ScriptedThenSeededRandom(0, 1, 1, 0));

            ResolveResult result = resolver.ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            // Order inside wave 1: the beams, then the conversion, then the clear.
            List<Type> order = new List<Type>();
            foreach (ResolveStep step in result.Steps)
            {
                if (step.Wave <= 1) order.Add(step.GetType());
            }

            Assert.That(order.GetRange(0, 4), Is.EqualTo(new[]
            {
                typeof(SwapStep), typeof(ColorBombFireStep), typeof(ConvertStep), typeof(ClearStep)
            }));

            ConvertStep convert = FindStep<ConvertStep>(result);
            Assert.That(convert.Tiles.Count, Is.EqualTo(4));
            Assert.That(convert.Tiles[0].TileId, Is.EqualTo(idOfGreenAt01), "A converted tile keeps its id: it is the same piece.");
            Assert.That(convert.Tiles[0].Position, Is.EqualTo(new GridPos(0, 1)));
            Assert.That(convert.Tiles[0].Special, Is.EqualTo(SpecialType.RocketHorizontal));
            Assert.That(convert.Tiles[1].Special, Is.EqualTo(SpecialType.RocketVertical));

            // Rockets: row 0 (swapped one, now at (0,0)), row 1, row 4, column 3, column 4.
            HashSet<GridPos> expected = Rectangle(0, 0, 4, 0);
            expected.UnionWith(Rectangle(0, 1, 4, 1));
            expected.UnionWith(Rectangle(0, 4, 4, 4));
            expected.UnionWith(Rectangle(3, 0, 3, 4));
            expected.UnionWith(Rectangle(4, 0, 4, 4));
            Assert.That(CellsOf(FindStep<ClearStep>(result)), Is.EquivalentTo(expected));
        }

        [Test]
        public void ColorBombPlusBomb_TurnsThemIntoBombs_ThenExplodesThemAll()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithSpecial(1, 0, SpecialType.Bomb);

            ResolveResult result = NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            // Bombs at (0,0) (the swapped one), (0,1), (4,2), (3,3), (2,4), each 3x3 and clipped at the edges.
            HashSet<GridPos> expected = Rectangle(0, 0, 1, 1);
            expected.UnionWith(Rectangle(0, 0, 1, 2));
            expected.UnionWith(Rectangle(3, 1, 4, 3));
            expected.UnionWith(Rectangle(2, 2, 4, 4));
            expected.UnionWith(Rectangle(1, 3, 3, 4));
            Assert.That(CellsOf(FindStep<ClearStep>(result)), Is.EquivalentTo(expected));
            Assert.That(FindStep<ConvertStep>(result).Tiles.Count, Is.EqualTo(4));
        }

        [Test]
        public void ColorBombPlusColorBomb_ClearsTheWholeBoard()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithColorBomb(0, 0).WithColorBomb(1, 0);

            ResolveResult result = NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            ClearStep clear = FindStep<ClearStep>(result);
            Assert.That(clear.Tiles.Count, Is.EqualTo(25));
            Assert.That(CellsOf(clear), Is.EquivalentTo(Rectangle(0, 0, 4, 4)));
        }

        [Test]
        public void ABombBlast_ThatCatchesAColorBomb_ClearsTheMostCommonColor_ThroughBoardResolver()
        {
            // Swapping the red bomb at (3,0) up with the blue tile at (3,1) sets the bomb off at (3,1).
            // Its 3x3 (x 2..3, y 0..2, clipped at the right edge) catches the ColorBomb at (2,1), which clears blue.
            Board board = WinnerBoard().WithSpecial(3, 0, SpecialType.Bomb).WithColorBomb(2, 1);

            ResolveResult result = NewResolver(new SystemRandom(1)).ResolveSwap(board, new GridPos(3, 0), new GridPos(3, 1));

            // Blue tiles after the swap: (0,2) (1,2) (1,1) (3,0) (2,0).
            HashSet<GridPos> expected = Rectangle(2, 0, 3, 2);
            expected.UnionWith(new[] { new GridPos(0, 2), new GridPos(1, 2), new GridPos(1, 1), new GridPos(3, 0), new GridPos(2, 0) });
            Assert.That(CellsOf(FindStep<ClearStep>(result)), Is.EquivalentTo(expected));
            Assert.That(FindStep<ColorBombFireStep>(result), Is.Not.Null);
        }
    }
}
