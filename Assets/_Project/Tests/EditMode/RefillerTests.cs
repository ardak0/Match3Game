using System;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class RefillerTests
    {
        private static TileColor[] Colors(int count)
        {
            TileColor[] colors = new TileColor[count];
            Array.Copy(TestBoards.AllColors, colors, count);
            return colors;
        }

        private static TileSpawn FindSpawn(SpawnStep step, int x, int y)
        {
            for (int i = 0; i < step.Spawns.Count; i++)
            {
                if (step.Spawns[i].To.X == x && step.Spawns[i].To.Y == y) return step.Spawns[i];
            }

            Assert.Fail("No spawn recorded for cell (" + x + "," + y + ")");
            return default;
        }

        [Test]
        public void Refill_FillsEveryEmptyCell()
        {
            Board board = TestBoards.FromRows(
                "R..",
                "RG.",
                "RGB");
            Refiller refiller = new Refiller(new SystemRandom(1), Colors(4));

            SpawnStep step = refiller.Refill(board, 1);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(board.Get(x, y), Is.Not.Null, "Cell (" + x + "," + y + ") is still empty.");
                }
            }

            Assert.That(step.Wave, Is.EqualTo(1));
            Assert.That(step.Spawns.Count, Is.EqualTo(3)); // exactly the 3 empty cells
        }

        [Test]
        public void Refill_LeavesExistingTilesUntouched()
        {
            Board board = TestBoards.FromRows("R.", "RG");
            Tile r = board.Get(0, 1);
            Tile g = board.Get(1, 0);
            Refiller refiller = new Refiller(new SystemRandom(1), Colors(4));

            refiller.Refill(board, 1);

            Assert.That(board.Get(0, 1), Is.SameAs(r));
            Assert.That(board.Get(1, 0), Is.SameAs(g));
        }

        [Test]
        public void Refill_OnAFullBoard_ReturnsNull()
        {
            Board board = TestBoards.FromRows("RG", "GR");
            Refiller refiller = new Refiller(new SystemRandom(1), Colors(4));

            Assert.That(refiller.Refill(board, 1), Is.Null);
        }

        [Test]
        public void Refill_UsesTheRandomNumbersInColumnThenBottomUpOrder()
        {
            // Empty cells, in fill order: (0,2) then (1,1) then (1,2).
            Board board = TestBoards.FromRows(
                "..",
                "R.",
                "RR");
            // Colors list is [Red, Blue]: 1 = Blue, 0 = Red.
            ScriptedRandom random = new ScriptedRandom(1, 0, 1);
            Refiller refiller = new Refiller(random, new[] { TileColor.Red, TileColor.Blue });

            refiller.Refill(board, 1);

            Assert.That(board.Get(0, 2).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(board.Get(1, 1).Color, Is.EqualTo(TileColor.Red));
            Assert.That(board.Get(1, 2).Color, Is.EqualTo(TileColor.Blue));
            Assert.That(random.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void Refill_OnlyUsesTheGivenColors()
        {
            Board board = TestBoards.FromRows("....", "....", "....", "....");
            TileColor[] colors = Colors(3);
            Refiller refiller = new Refiller(new SystemRandom(5), colors);

            refiller.Refill(board, 1);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(colors, Contains.Item(board.Get(x, y).Color));
                }
            }
        }

        [Test]
        public void Refill_GivesNewTilesUniqueIds()
        {
            Board board = TestBoards.FromRows("R..", "R..", "RGB");
            Refiller refiller = new Refiller(new SystemRandom(2), Colors(4));

            refiller.Refill(board, 1);

            HashSet<int> ids = new HashSet<int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(ids.Add(board.Get(x, y).Id), Is.True, "Duplicate id at (" + x + "," + y + ").");
                }
            }
        }

        [Test]
        public void Spawns_StartAboveTheBoard_QueuedPerColumn()
        {
            Board board = TestBoards.FromRows(
                "..",
                "R.",
                "RR");
            Refiller refiller = new Refiller(new SystemRandom(1), Colors(4));

            SpawnStep step = refiller.Refill(board, 1);

            Assert.That(board.Height, Is.EqualTo(3));
            Assert.That(FindSpawn(step, 0, 2).FromY, Is.EqualTo(3)); // only new tile in column 0
            Assert.That(FindSpawn(step, 1, 1).FromY, Is.EqualTo(3)); // lowest new tile in column 1 starts first
            Assert.That(FindSpawn(step, 1, 2).FromY, Is.EqualTo(4)); // the one above it queues behind
        }

        [Test]
        public void Spawn_RecordsTheTileThatWasPlacedOnTheBoard()
        {
            Board board = TestBoards.FromRows(".");
            Refiller refiller = new Refiller(new SystemRandom(1), Colors(4));

            SpawnStep step = refiller.Refill(board, 1);

            TileSpawn spawn = step.Spawns[0];
            Tile placed = board.Get(0, 0);
            Assert.That(spawn.TileId, Is.EqualTo(placed.Id));
            Assert.That(spawn.Color, Is.EqualTo(placed.Color));
            Assert.That(spawn.To, Is.EqualTo(new GridPos(0, 0)));
        }

        [Test]
        public void SameSeed_GivesTheSameRefill()
        {
            Board first = TestBoards.FromRows("....", "....");
            Board second = TestBoards.FromRows("....", "....");

            new Refiller(new SystemRandom(9), Colors(5)).Refill(first, 1);
            new Refiller(new SystemRandom(9), Colors(5)).Refill(second, 1);

            for (int y = 0; y < first.Height; y++)
            {
                for (int x = 0; x < first.Width; x++)
                {
                    Assert.That(second.Get(x, y).Color, Is.EqualTo(first.Get(x, y).Color));
                }
            }
        }

        [Test]
        public void NoColors_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Refiller(new SystemRandom(1), new TileColor[0]));
        }
    }
}
