using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class SpecialResolverTests
    {
        private SpecialResolver _resolver;
        private MatchFinder _finder;

        [SetUp]
        public void SetUp()
        {
            _resolver = new SpecialResolver();
            _finder = new MatchFinder();
        }

        private Match FindTheOnlyMatch(Board board)
        {
            List<Match> matches = new List<Match>();
            _finder.FindMatches(board, matches);
            Assert.That(matches.Count, Is.EqualTo(1), "The test board should hold exactly one match.");
            return matches[0];
        }

        // Marks the seed cells (depth 0) and sets off every special among them, like BoardResolver does.
        private ClearSet Activate(Board board, params GridPos[] seeds)
        {
            ClearSet set = new ClearSet();
            set.Reset(board);
            foreach (GridPos seed in seeds)
            {
                set.Mark(seed, 0);
            }

            _resolver.ActivateSpecials(board, set);
            return set;
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

        private static readonly GridPos NoSwap = new GridPos(-1, -1);

        // ---------- creation: which special ----------

        [Test]
        public void Horizontal4_CreatesAVerticalRocket_InTheMiddleOfTheRun()
        {
            Board board = TestBoards.FromRows(
                "GBGB",
                "RRRR");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.RocketVertical)); // it clears its column
            Assert.That(creation.Color, Is.EqualTo(TileColor.Red));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(2, 0)));
        }

        [Test]
        public void Vertical4_CreatesAHorizontalRocket_InTheMiddleOfTheRun()
        {
            Board board = TestBoards.FromRows(
                "RG",
                "RB",
                "RG",
                "RB");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.RocketHorizontal)); // it clears its row
            Assert.That(creation.Position, Is.EqualTo(new GridPos(0, 2)));
        }

        [Test]
        public void StraightRunOf3_CreatesNothing()
        {
            Board board = TestBoards.FromRows(
                "GBG",
                "RRR");

            Assert.That(_resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out _), Is.False);
        }

        [Test]
        public void StraightRunOf5_AlsoCreatesARocket_AsTheRuleIs4OrMore()
        {
            Board board = TestBoards.FromRows(
                "GBGBG",
                "RRRRR");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.RocketVertical));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(2, 0)));
        }

        [Test]
        public void LShape_CreatesABomb_AtTheCorner()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "RBG",
                "RRR");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.Bomb));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(0, 0)));
        }

        [Test]
        public void TShape_CreatesABomb_AtTheJunction()
        {
            Board board = TestBoards.FromRows(
                "RRR",
                "GRB",
                "BRG");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.Bomb));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(1, 2)));
        }

        [Test]
        public void PlusShape_CreatesABomb_AtTheCenter()
        {
            Board board = TestBoards.FromRows(
                "GRB",
                "RRR",
                "BRG");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.Bomb));
            Assert.That(creation.Position, Is.EqualTo(new GridPos(1, 1)));
        }

        [Test]
        public void ABombShapeBeatsALongRun()
        {
            // A run of 4 along the bottom with a vertical run of 3 standing on its left end: L shape, so a bomb, not a rocket.
            Board board = TestBoards.FromRows(
                "RGBGB",
                "RBGBG",
                "RRRRB");

            bool created = _resolver.TryGetCreation(FindTheOnlyMatch(board), false, NoSwap, NoSwap, out SpecialCreation creation);

            Assert.That(created, Is.True);
            Assert.That(creation.Special, Is.EqualTo(SpecialType.Bomb));
        }

        // ---------- creation: where ----------

        [Test]
        public void TheSpecialAppearsInTheSwappedCell_WhenThatCellIsInTheMatch()
        {
            Board board = TestBoards.FromRows(
                "GBGB",
                "RRRR");
            GridPos swapB = new GridPos(1, 0);

            _resolver.TryGetCreation(FindTheOnlyMatch(board), true, new GridPos(1, 1), swapB, out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(swapB));
        }

        [Test]
        public void IfOnlyCellAIsInTheMatch_TheSpecialAppearsThere()
        {
            Board board = TestBoards.FromRows(
                "GBGB",
                "RRRR");
            GridPos swapA = new GridPos(3, 0);

            _resolver.TryGetCreation(FindTheOnlyMatch(board), true, swapA, new GridPos(3, 1), out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(swapA));
        }

        [Test]
        public void IfBothSwappedCellsAreInTheMatch_CellBWins()
        {
            Board board = TestBoards.FromRows(
                "GBGB",
                "RRRR");

            _resolver.TryGetCreation(FindTheOnlyMatch(board), true, new GridPos(0, 0), new GridPos(1, 0), out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(new GridPos(1, 0)));
        }

        [Test]
        public void IfNeitherSwappedCellIsInTheMatch_TheSpecialAppearsInTheMiddle()
        {
            Board board = TestBoards.FromRows(
                "GBGB",
                "RRRR");

            _resolver.TryGetCreation(FindTheOnlyMatch(board), true, new GridPos(0, 1), new GridPos(1, 1), out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(new GridPos(2, 0)));
        }

        [Test]
        public void ABombAppearsInTheSwappedCell_NotAtTheCorner_WhenTheSwappedCellIsInTheMatch()
        {
            Board board = TestBoards.FromRows(
                "RGB",
                "RBG",
                "RRR");
            GridPos swapB = new GridPos(2, 0);

            _resolver.TryGetCreation(FindTheOnlyMatch(board), true, new GridPos(2, 1), swapB, out SpecialCreation creation);

            Assert.That(creation.Position, Is.EqualTo(swapB));
        }

        // ---------- activation ----------

        [Test]
        public void HorizontalRocket_ClearsItsWholeRow()
        {
            Board board = TestBoards.Diagonal(5, 3, 5).WithSpecial(2, 1, SpecialType.RocketHorizontal);

            ClearSet set = Activate(board, new GridPos(2, 1));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(0, 1, 4, 1)));
            Assert.That(set.DepthAt(new GridPos(2, 1)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(0, 1)), Is.EqualTo(1));
        }

        [Test]
        public void VerticalRocket_ClearsItsWholeColumn()
        {
            Board board = TestBoards.Diagonal(5, 4, 5).WithSpecial(3, 2, SpecialType.RocketVertical);

            ClearSet set = Activate(board, new GridPos(3, 2));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(3, 0, 3, 3)));
        }

        [Test]
        public void Bomb_Clears3x3()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithSpecial(2, 2, SpecialType.Bomb);

            ClearSet set = Activate(board, new GridPos(2, 2));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(1, 1, 3, 3)));
        }

        [Test]
        public void Bomb_InACorner_IsClippedAtTheEdges()
        {
            Board board = TestBoards.Diagonal(4, 4, 4).WithSpecial(0, 0, SpecialType.Bomb);

            ClearSet set = Activate(board, new GridPos(0, 0));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(0, 0, 1, 1)));
        }

        [Test]
        public void Bomb_OnAnEdge_IsClippedOnThatSideOnly()
        {
            Board board = TestBoards.Diagonal(5, 5, 5).WithSpecial(2, 4, SpecialType.Bomb);

            ClearSet set = Activate(board, new GridPos(2, 4));

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(1, 3, 3, 4)));
        }

        [Test]
        public void ANormalTileInTheSet_ClearsNothingExtra()
        {
            Board board = TestBoards.Diagonal(5, 5, 5);

            ClearSet set = Activate(board, new GridPos(1, 1), new GridPos(2, 1));

            Assert.That(set.Count, Is.EqualTo(2));
        }

        // ---------- chain reactions ----------

        [Test]
        public void ASpecialHitByAnotherSpecial_GoesOffToo_AndTheChainGetsDeeper()
        {
            // Bomb (1,1) hits the vertical rocket at (2,2); its column hits the horizontal rocket at (2,4); its row is row 4.
            Board board = TestBoards.Diagonal(5, 5, 5)
                .WithSpecial(1, 1, SpecialType.Bomb)
                .WithSpecial(2, 2, SpecialType.RocketVertical)
                .WithSpecial(2, 4, SpecialType.RocketHorizontal);

            ClearSet set = Activate(board, new GridPos(1, 1));

            HashSet<GridPos> expected = Rectangle(0, 0, 2, 2); // the bomb's 3x3
            expected.UnionWith(Rectangle(2, 0, 2, 4));         // the rocket's column
            expected.UnionWith(Rectangle(0, 4, 4, 4));         // the second rocket's row
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
            Assert.That(set.Count, Is.EqualTo(expected.Count), "No cell may be in the set twice.");

            Assert.That(set.DepthAt(new GridPos(1, 1)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(2, 2)), Is.EqualTo(1)); // hit by the bomb
            Assert.That(set.DepthAt(new GridPos(2, 3)), Is.EqualTo(2)); // hit by the first rocket
            Assert.That(set.DepthAt(new GridPos(2, 4)), Is.EqualTo(2));
            Assert.That(set.DepthAt(new GridPos(0, 4)), Is.EqualTo(3)); // hit by the second rocket
        }

        [Test]
        public void TwoBombsNextToEachOther_EachGoOffOnce_AndTheChainEnds()
        {
            Board board = TestBoards.Diagonal(5, 5, 5)
                .WithSpecial(1, 1, SpecialType.Bomb)
                .WithSpecial(2, 1, SpecialType.Bomb);

            ClearSet set = Activate(board, new GridPos(1, 1));

            // 3x3 around (1,1) plus 3x3 around (2,1) = columns 0..3, rows 0..2.
            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(0, 0, 3, 2)));
        }

        // ---------- combos ----------

        [Test]
        public void RocketPlusRocket_ClearsARowAndAColumn()
        {
            Board board = TestBoards.Diagonal(7, 7, 6);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.RocketHorizontal, SpecialType.RocketVertical);

            HashSet<GridPos> expected = Rectangle(0, 3, 6, 3);
            expected.UnionWith(Rectangle(3, 0, 3, 6));
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
            Assert.That(set.Count, Is.EqualTo(13));
        }

        [Test]
        public void TwoRocketsOfTheSameKind_AlsoMakeACross()
        {
            Board board = TestBoards.Diagonal(7, 7, 6);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.RocketHorizontal, SpecialType.RocketHorizontal);

            Assert.That(set.Count, Is.EqualTo(13));
        }

        [Test]
        public void RocketPlusBomb_Clears3RowsAnd3Columns()
        {
            Board board = TestBoards.Diagonal(7, 7, 6);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.Bomb, SpecialType.RocketVertical);

            HashSet<GridPos> expected = Rectangle(0, 2, 6, 4);
            expected.UnionWith(Rectangle(2, 0, 4, 6));
            Assert.That(CellsOf(set), Is.EquivalentTo(expected));
            Assert.That(set.Count, Is.EqualTo(33)); // 21 + 21 - the 9 cells they share
        }

        [Test]
        public void BombPlusBomb_Clears5x5()
        {
            Board board = TestBoards.Diagonal(7, 7, 6);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.Bomb, SpecialType.Bomb);

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(1, 1, 5, 5)));
        }

        [Test]
        public void BombPlusBomb_InACorner_IsClipped()
        {
            Board board = TestBoards.Diagonal(7, 7, 6);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(1, 0), new GridPos(0, 0), SpecialType.Bomb, SpecialType.Bomb);

            Assert.That(CellsOf(set), Is.EquivalentTo(Rectangle(0, 0, 2, 2)));
        }

        [Test]
        public void TheTwoComboTiles_AreUsedUp_AndCountAsAlreadyActivated()
        {
            Board board = TestBoards.Diagonal(7, 7, 6)
                .WithSpecial(2, 3, SpecialType.Bomb)
                .WithSpecial(3, 3, SpecialType.RocketVertical);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.Bomb, SpecialType.RocketVertical);

            Assert.That(set.IsMarked(new GridPos(2, 3)), Is.True);
            Assert.That(set.IsMarked(new GridPos(3, 3)), Is.True);
            Assert.That(set.IsActivated(new GridPos(2, 3)), Is.True);
            Assert.That(set.IsActivated(new GridPos(3, 3)), Is.True);
            Assert.That(set.DepthAt(new GridPos(2, 3)), Is.EqualTo(0));
            Assert.That(set.DepthAt(new GridPos(0, 3)), Is.EqualTo(1));
        }

        [Test]
        public void AComboThatHitsAnotherSpecial_SetsItOff()
        {
            // Bomb + Bomb around (3,3) covers x 1..5, y 1..5. The rocket at (5,5) is inside, so its row 5 is cleared too.
            Board board = TestBoards.Diagonal(7, 7, 6).WithSpecial(5, 5, SpecialType.RocketHorizontal);
            ClearSet set = new ClearSet();
            set.Reset(board);

            _resolver.MarkCombo(board, set, new GridPos(2, 3), new GridPos(3, 3), SpecialType.Bomb, SpecialType.Bomb);
            _resolver.ActivateSpecials(board, set);

            Assert.That(set.IsMarked(new GridPos(0, 5)), Is.True);
            Assert.That(set.IsMarked(new GridPos(6, 5)), Is.True);
            Assert.That(set.Count, Is.EqualTo(25 + 2)); // the square plus the two row-5 cells outside it
        }
    }
}
