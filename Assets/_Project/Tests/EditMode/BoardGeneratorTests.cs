using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class BoardGeneratorTests
    {
        private static readonly int[][] Sizes =
        {
            new[] { 8, 8 },
            new[] { 6, 9 },
            new[] { 5, 7 }
        };

        private static TileColor[] FirstColors(int count)
        {
            TileColor[] colors = new TileColor[count];
            Array.Copy(TestBoards.AllColors, colors, count);
            return colors;
        }

        private static Board Generate(int seed, int width, int height, int colorCount)
        {
            BoardGenerator generator = new BoardGenerator(new SystemRandom(seed));
            return generator.Generate(width, height, FirstColors(colorCount));
        }

        [Test]
        public void GeneratedBoards_HaveNoInitialMatches_ForManySeeds()
        {
            MatchFinder finder = new MatchFinder();
            List<Match> matches = new List<Match>();

            for (int seed = 0; seed < 200; seed++)
            {
                foreach (int[] size in Sizes)
                {
                    for (int colorCount = 3; colorCount <= 6; colorCount++)
                    {
                        Board board = Generate(seed, size[0], size[1], colorCount);
                        finder.FindMatches(board, matches);

                        Assert.That(matches, Is.Empty,
                            "Seed " + seed + ", " + size[0] + "x" + size[1] + ", " + colorCount + " colors had a match.");
                    }
                }
            }
        }

        [Test]
        public void GeneratedBoards_HaveAtLeastOnePossibleMove_ForManySeeds()
        {
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());

            for (int seed = 0; seed < 200; seed++)
            {
                foreach (int[] size in Sizes)
                {
                    for (int colorCount = 3; colorCount <= 6; colorCount++)
                    {
                        Board board = Generate(seed, size[0], size[1], colorCount);

                        Assert.That(moveFinder.HasPossibleMove(board), Is.True,
                            "Seed " + seed + ", " + size[0] + "x" + size[1] + ", " + colorCount + " colors had no move.");
                    }
                }
            }
        }

        [Test]
        public void GeneratedBoard_IsCompletelyFilled_WithUniqueTileIds()
        {
            Board board = Generate(7, 8, 8, 5);
            HashSet<int> ids = new HashSet<int>();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = board.Get(x, y);
                    Assert.That(tile, Is.Not.Null, "Cell (" + x + "," + y + ") is empty.");
                    Assert.That(ids.Add(tile.Id), Is.True, "Duplicate tile id " + tile.Id);
                }
            }

            Assert.That(ids.Count, Is.EqualTo(64));
        }

        [Test]
        public void GeneratedBoard_OnlyUsesTheRequestedColors()
        {
            Board board = Generate(3, 8, 8, 4);
            TileColor[] allowed = FirstColors(4);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(allowed, Contains.Item(board.Get(x, y).Color));
                }
            }
        }

        [Test]
        public void SameSeed_GivesTheSameBoard()
        {
            Board first = Generate(42, 8, 8, 5);
            Board second = Generate(42, 8, 8, 5);

            for (int y = 0; y < first.Height; y++)
            {
                for (int x = 0; x < first.Width; x++)
                {
                    Assert.That(second.Get(x, y).Color, Is.EqualTo(first.Get(x, y).Color));
                }
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentBoards()
        {
            Board first = Generate(1, 8, 8, 5);
            Board second = Generate(2, 8, 8, 5);

            bool anyDifference = false;
            for (int y = 0; y < first.Height && !anyDifference; y++)
            {
                for (int x = 0; x < first.Width; x++)
                {
                    if (first.Get(x, y).Color != second.Get(x, y).Color)
                    {
                        anyDifference = true;
                        break;
                    }
                }
            }

            Assert.That(anyDifference, Is.True);
        }

        [Test]
        public void TooFewColors_Throws()
        {
            BoardGenerator generator = new BoardGenerator(new SystemRandom(1));

            Assert.Throws<ArgumentException>(() => generator.Generate(8, 8, FirstColors(2)));
        }
    }
}
