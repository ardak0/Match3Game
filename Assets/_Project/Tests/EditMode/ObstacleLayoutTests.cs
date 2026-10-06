using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class ObstacleLayoutTests
    {
        [Test]
        public void Parse_ReadsEveryLetter_TopRowFirst()
        {
            // The first string is the TOP row, so it ends up at y = 1 on a board that is 2 rows high.
            string[] rows =
            {
                "CDIJL",
                "....."
            };

            bool ok = ObstacleLayout.TryParse(rows, 5, 2, out ObstacleLayout layout, out string error);

            Assert.That(ok, Is.True, error);
            Assert.That(layout.Get(0, 1), Is.EqualTo(new Obstacle(ObstacleType.Crate, 1)));
            Assert.That(layout.Get(1, 1), Is.EqualTo(new Obstacle(ObstacleType.Crate, 2)));
            Assert.That(layout.Get(2, 1), Is.EqualTo(new Obstacle(ObstacleType.Ice, 1)));
            Assert.That(layout.Get(3, 1), Is.EqualTo(new Obstacle(ObstacleType.Ice, 2)));
            Assert.That(layout.Get(4, 1), Is.EqualTo(new Obstacle(ObstacleType.Chain, 1)));
            Assert.That(layout.Get(0, 0).IsNone, Is.True);
        }

        [Test]
        public void Parse_NullOrEmptyRows_MeansNoObstacles()
        {
            Assert.That(ObstacleLayout.TryParse(null, 6, 6, out ObstacleLayout fromNull, out _), Is.True);
            Assert.That(ObstacleLayout.TryParse(new string[0], 6, 6, out ObstacleLayout fromEmpty, out _), Is.True);

            Assert.That(fromNull.Count(ObstacleType.Crate), Is.EqualTo(0));
            Assert.That(fromEmpty.Get(3, 3).IsNone, Is.True);
        }

        [Test]
        public void Parse_WrongRowCount_ExplainsTheProblem()
        {
            bool ok = ObstacleLayout.TryParse(new[] { "....", "...." }, 4, 3, out _, out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("3 rows"));
            Assert.That(error, Does.Contain("2"));
        }

        [Test]
        public void Parse_WrongRowWidth_NamesTheRow()
        {
            bool ok = ObstacleLayout.TryParse(new[] { "....", "...", "...." }, 4, 3, out _, out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Row 2"));
            Assert.That(error, Does.Contain("4"));
        }

        [Test]
        public void Parse_UnknownLetter_NamesTheLetterAndTheCell()
        {
            bool ok = ObstacleLayout.TryParse(new[] { "....", ".X..", "...." }, 4, 3, out _, out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("'X'"));
            Assert.That(error, Does.Contain("row 2"));
            Assert.That(error, Does.Contain("column 2"));
        }

        [Test]
        public void Count_CountsEachObstacleOnce_NoMatterItsHp()
        {
            ObstacleLayout.TryParse(new[] { "CDI", "J.L", "CCI" }, 3, 3, out ObstacleLayout layout, out _);

            Assert.That(layout.Count(ObstacleType.Crate), Is.EqualTo(4));
            Assert.That(layout.Count(ObstacleType.Ice), Is.EqualTo(3));
            Assert.That(layout.Count(ObstacleType.Chain), Is.EqualTo(1));
        }

        [Test]
        public void ApplyTo_PutsTheObstaclesOnTheBoard()
        {
            ObstacleLayout.TryParse(new[] { "...", ".D.", "I.L" }, 3, 3, out ObstacleLayout layout, out _);
            Board board = new Board(3, 3);

            layout.ApplyTo(board);

            Assert.That(board.GetObstacle(1, 1), Is.EqualTo(new Obstacle(ObstacleType.Crate, 2)));
            Assert.That(board.GetObstacle(0, 0), Is.EqualTo(new Obstacle(ObstacleType.Ice, 1)));
            Assert.That(board.IsChained(2, 0), Is.True);
            Assert.That(board.GetObstacle(2, 2).IsNone, Is.True);
        }

        [Test]
        public void ApplyTo_WithTheWrongBoardSize_Throws()
        {
            ObstacleLayout.TryParse(new[] { "..", ".." }, 2, 2, out ObstacleLayout layout, out _);

            Assert.Throws<System.ArgumentException>(() => layout.ApplyTo(new Board(3, 3)));
        }

        [Test]
        public void Unfillable_CellUnderACrateWithCratesOrWallsOnBothDiagonals_IsFound()
        {
            // The cell under the middle crate can get nothing from above, and both upper diagonals are crates.
            // (The cells under the outer crates of the row still have a free diagonal.)
            ObstacleLayout.TryParse(new[] { ".CCC.", ".....", "....." }, 5, 3, out ObstacleLayout layout, out _);

            bool found = layout.TryFindUnfillableCell(out GridPos cell);

            Assert.That(found, Is.True);
            Assert.That(cell, Is.EqualTo(new GridPos(2, 1)));
        }

        [Test]
        public void Unfillable_CornerCrateWithACrateNextToIt_IsFound()
        {
            // The cell under the corner crate has a wall on one diagonal and a crate on the other.
            ObstacleLayout.TryParse(new[] { "CC.", "...", "..." }, 3, 3, out ObstacleLayout layout, out _);

            Assert.That(layout.TryFindUnfillableCell(out GridPos cell), Is.True);
            Assert.That(cell, Is.EqualTo(new GridPos(0, 1)));
        }

        [Test]
        public void Unfillable_CrateWithAFreeDiagonal_IsFine()
        {
            ObstacleLayout.TryParse(new[] { ".C.", "...", "..." }, 3, 3, out ObstacleLayout layout, out _);

            Assert.That(layout.TryFindUnfillableCell(out _), Is.False);
        }

        [Test]
        public void Unfillable_ACrateAboveACrate_NeedsNoFilling()
        {
            // The cell under a crate is itself a crate, so nothing has to fill it.
            ObstacleLayout.TryParse(new[] { ".C.", ".C.", "..." }, 3, 3, out ObstacleLayout layout, out _);

            Assert.That(layout.TryFindUnfillableCell(out _), Is.False);
        }
    }
}
