using System;
using Match3.Core;
using NUnit.Framework;

namespace Match3.Tests
{
    public class BoardTests
    {
        [Test]
        public void NewBoard_IsEmpty()
        {
            Board board = new Board(4, 3);

            Assert.That(board.Width, Is.EqualTo(4));
            Assert.That(board.Height, Is.EqualTo(3));
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(board.Get(x, y), Is.Null);
                }
            }
        }

        [Test]
        public void SetThenGet_ReturnsSameTile()
        {
            Board board = new Board(3, 3);
            Tile tile = board.NewTile(TileColor.Blue);

            board.Set(2, 1, tile);

            Assert.That(board.Get(2, 1), Is.SameAs(tile));
            Assert.That(board.Get(new GridPos(2, 1)), Is.SameAs(tile));
        }

        [Test]
        public void IsInside_ChecksAllFourEdges()
        {
            Board board = new Board(3, 2);

            Assert.That(board.IsInside(0, 0), Is.True);
            Assert.That(board.IsInside(2, 1), Is.True);
            Assert.That(board.IsInside(-1, 0), Is.False);
            Assert.That(board.IsInside(0, -1), Is.False);
            Assert.That(board.IsInside(3, 0), Is.False);
            Assert.That(board.IsInside(0, 2), Is.False);
        }

        [Test]
        public void Get_OutsideBoard_Throws()
        {
            Board board = new Board(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.Get(3, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.Get(0, -1));
        }

        [Test]
        public void Set_OutsideBoard_Throws()
        {
            Board board = new Board(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.Set(5, 5, null));
        }

        [Test]
        public void Constructor_RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(3, 0));
        }

        [Test]
        public void Swap_ExchangesTwoTiles()
        {
            Board board = new Board(2, 1);
            Tile red = board.NewTile(TileColor.Red);
            Tile blue = board.NewTile(TileColor.Blue);
            board.Set(0, 0, red);
            board.Set(1, 0, blue);

            board.Swap(new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(board.Get(0, 0), Is.SameAs(blue));
            Assert.That(board.Get(1, 0), Is.SameAs(red));
        }

        [Test]
        public void Swap_WithEmptyCell_MovesTileAndLeavesNull()
        {
            Board board = new Board(2, 1);
            Tile red = board.NewTile(TileColor.Red);
            board.Set(0, 0, red);

            board.Swap(new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(board.Get(0, 0), Is.Null);
            Assert.That(board.Get(1, 0), Is.SameAs(red));
        }

        [Test]
        public void NewTile_GivesEveryTileADifferentId()
        {
            Board board = new Board(2, 2);

            Tile a = board.NewTile(TileColor.Red);
            Tile b = board.NewTile(TileColor.Red);
            Tile c = board.NewTile(TileColor.Green, SpecialType.Bomb);

            Assert.That(a.Id, Is.Not.EqualTo(b.Id));
            Assert.That(b.Id, Is.Not.EqualTo(c.Id));
            Assert.That(a.Id, Is.Not.EqualTo(c.Id));
            Assert.That(c.Special, Is.EqualTo(SpecialType.Bomb));
        }

        [Test]
        public void NewTile_DoesNotPlaceTheTile()
        {
            Board board = new Board(2, 2);

            board.NewTile(TileColor.Red);

            Assert.That(board.Get(0, 0), Is.Null);
        }
    }
}
