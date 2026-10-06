using System;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class ObstacleRowsTests
    {
        [Test]
        public void CreateEmpty_IsAGridOfDots()
        {
            string[] rows = ObstacleRows.CreateEmpty(4, 3);

            Assert.That(rows, Is.EqualTo(new[] { "....", "....", "...." }));
        }

        [Test]
        public void IsEmpty_TrueForNullNoRowsAndDotsOnly()
        {
            Assert.That(ObstacleRows.IsEmpty(null), Is.True);
            Assert.That(ObstacleRows.IsEmpty(new string[0]), Is.True);
            Assert.That(ObstacleRows.IsEmpty(new[] { "...", "..." }), Is.True);
        }

        [Test]
        public void IsEmpty_FalseAsSoonAsOneObstacleExists()
        {
            Assert.That(ObstacleRows.IsEmpty(new[] { "...", ".C." }), Is.False);
        }

        [Test]
        public void SetCell_RowZeroIsTheTopRow()
        {
            string[] rows = ObstacleRows.SetCell(ObstacleRows.CreateEmpty(3, 3), 3, 3, 2, 0, ObstacleRows.Crate1);

            Assert.That(rows, Is.EqualTo(new[] { "..C", "...", "..." }));
        }

        [Test]
        public void SetCell_DoesNotChangeTheGridItWasGiven()
        {
            string[] original = { "...", "...", "..." };

            ObstacleRows.SetCell(original, 3, 3, 1, 1, ObstacleRows.Ice2);

            Assert.That(original, Is.EqualTo(new[] { "...", "...", "..." }));
        }

        [Test]
        public void SetCell_OnAMissingGridCreatesAFullGrid()
        {
            string[] rows = ObstacleRows.SetCell(new string[0], 3, 2, 1, 1, ObstacleRows.Chain);

            Assert.That(rows, Is.EqualTo(new[] { "...", ".L." }));
        }

        [Test]
        public void SetCell_WithTheEmptyLetterErasesACell()
        {
            string[] rows = ObstacleRows.SetCell(new[] { ".C.", "..." }, 3, 2, 1, 0, ObstacleRows.Empty);

            Assert.That(rows, Is.EqualTo(new[] { "...", "..." }));
        }

        [Test]
        public void SetCell_RejectsUnknownLettersAndCellsOutsideTheBoard()
        {
            string[] rows = ObstacleRows.CreateEmpty(3, 3);

            Assert.Throws<ArgumentException>(() => ObstacleRows.SetCell(rows, 3, 3, 0, 0, 'X'));
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleRows.SetCell(rows, 3, 3, 3, 0, 'C'));
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleRows.SetCell(rows, 3, 3, 0, -1, 'C'));
        }

        [Test]
        public void Resize_Bigger_KeepsTheTopLeftAndAddsEmptyCells()
        {
            string[] rows = ObstacleRows.Resize(new[] { "C.", ".D" }, 3, 3);

            Assert.That(rows, Is.EqualTo(new[] { "C..", ".D.", "..." }));
        }

        [Test]
        public void Resize_Smaller_DropsCellsThatNoLongerFit()
        {
            string[] rows = ObstacleRows.Resize(new[] { "C.L", ".D.", "I.J" }, 2, 2);

            Assert.That(rows, Is.EqualTo(new[] { "C.", ".D" }));
        }

        [Test]
        public void Resize_OfNothing_GivesAnEmptyGrid()
        {
            Assert.That(ObstacleRows.Resize(null, 2, 2), Is.EqualTo(new[] { "..", ".." }));
        }

        [Test]
        public void GetCell_ReadsTheLetterAndTreatsOutsideAsEmpty()
        {
            string[] rows = { "C.", ".D" };

            Assert.That(ObstacleRows.GetCell(rows, 0, 0), Is.EqualTo('C'));
            Assert.That(ObstacleRows.GetCell(rows, 1, 1), Is.EqualTo('D'));
            Assert.That(ObstacleRows.GetCell(rows, 5, 5), Is.EqualTo('.'));
            Assert.That(ObstacleRows.GetCell(null, 0, 0), Is.EqualTo('.'));
        }

        [Test]
        public void Normalize_TurnsAnEmptyGridIntoNoRows_AndKeepsARealOne()
        {
            Assert.That(ObstacleRows.Normalize(new[] { "..", ".." }), Is.Empty);
            Assert.That(ObstacleRows.Normalize(new[] { "..", ".C" }), Is.EqualTo(new[] { "..", ".C" }));
        }

        [Test]
        public void RowFromTop_ConvertsTheBoardsBottomUpYToATextRow()
        {
            Assert.That(ObstacleRows.RowFromTop(0, 5), Is.EqualTo(4)); // the bottom of the board is the last text row
            Assert.That(ObstacleRows.RowFromTop(4, 5), Is.EqualTo(0));
        }

        [Test]
        public void EveryLetterTheLayoutParserKnowsIsValid()
        {
            foreach (char letter in ".CDIJL")
            {
                Assert.That(ObstacleRows.IsValidLetter(letter), Is.True, letter.ToString());
                Assert.That(ObstacleLayout.TryParse(new[] { new string(letter, 4), "....", "....", "...." }, 4, 4, out _, out _), Is.True, letter.ToString());
            }

            Assert.That(ObstacleRows.IsValidLetter('X'), Is.False);
        }
    }
}
