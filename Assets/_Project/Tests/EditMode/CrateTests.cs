using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    public class CrateTests
    {
        // A 5x4 board with no match anywhere (rows are written top row first). The bottom row is "RRGRP":
        // swapping (2,0) and (3,0) makes the red match (0,0)-(1,0)-(2,0).
        // The crate letter in the tile rows is '.', because a crate takes the cell (WithObstacles adds the crate itself).
        private static Board BoardWithCrateAt11(string crate)
        {
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "B.PRG",
                "RRGRP");
            return board.WithObstacles(
                ".....",
                ".....",
                "." + crate + "...",
                ".....");
        }

        private static readonly GridPos MatchSwapA = new GridPos(2, 0);
        private static readonly GridPos MatchSwapB = new GridPos(3, 0);

        [Test]
        public void Crate_NextToAClearedTile_LosesOneHp()
        {
            Board board = BoardWithCrateAt11("D"); // 2 HP

            ResolveResult result = ResolverWithRefill(3, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(result.IsValid, Is.True);
            ObstacleDamagedStep damaged = DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 1, 1);
            Assert.That(damaged, Is.Not.Null, "The crate next to the cleared tile (1,0) took no damage.");
            Assert.That(damaged.Type, Is.EqualTo(ObstacleType.Crate));
            Assert.That(damaged.HpLeft, Is.EqualTo(1));
            Assert.That(board.GetObstacle(1, 1), Is.EqualTo(new Obstacle(ObstacleType.Crate, 1)), Describe(board));
            Assert.That(board.Get(1, 1), Is.Null, "A crate cell never holds a tile.");
        }

        [Test]
        public void Crate_WithOneHp_IsDestroyed_AndItsCellIsFilledByGravity()
        {
            Board board = BoardWithCrateAt11("C"); // 1 HP

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Crate, 1, 1), Is.True);
            Assert.That(board.HasCrate(1, 1), Is.False);
            Assert.That(board.Get(1, 1), Is.Not.Null, "The empty crate cell must be filled by gravity.\n" + Describe(board));
            Assert.That(StepsOf<ObstacleDamagedStep>(result, 1), Is.Empty, "The last HP is a destroyed step, not a damaged step.");
        }

        [Test]
        public void Crate_FarFromTheMatch_IsUntouched()
        {
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPR.",
                "RRGRP").WithObstacles(
                ".....",
                ".....",
                "....D",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(StepsOf<ObstacleDamagedStep>(result, 1), Is.Empty);
            Assert.That(StepsOf<ObstacleDestroyedStep>(result, 1), Is.Empty);
        }

        [Test]
        public void Crate_OnlyDiagonallyNextToAClearedTile_IsUntouched()
        {
            // The crate at (3,1) touches the cleared tile (2,0) only at a corner.
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYP.G",
                "RRGRP").WithObstacles(
                ".....",
                ".....",
                "...D.",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 3, 1), Is.Null);
            Assert.That(board.GetObstacle(3, 1).Hp, Is.EqualTo(2));
        }

        [Test]
        public void Crate_WithSeveralClearedNeighbors_LosesOnlyOneHpPerWave()
        {
            // Swapping (0,0) and (1,0) puts a red tile in the corner of an L:
            // red at (1,0)-(2,0)-(3,0) and at (1,0)-(1,1)-(1,2). The crate at (2,1) touches two cleared tiles, (2,0) and (1,1).
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BR.YG",
                "RGRRB").WithObstacles(
                ".....",
                ".....",
                "..D..",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.True);
            List<ObstacleDamagedStep> damaged = StepsOf<ObstacleDamagedStep>(result, 1);
            Assert.That(damaged.Count, Is.EqualTo(1), "Two cleared neighbors must still be only one hit.");
            Assert.That(damaged[0].HpLeft, Is.EqualTo(1));
            Assert.That(StepsOf<ObstacleDestroyedStep>(result, 1), Is.Empty);
        }

        [Test]
        public void Crate_WithTwoHp_NeedsTwoWaves()
        {
            Board board = BoardWithCrateAt11("D");
            ResolveResult first = ResolverWithRefill(3, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);
            Assert.That(board.GetObstacle(1, 1).Hp, Is.EqualTo(1), "Precondition: one hit leaves 1 HP.\n" + Describe(board));

            // A second match next to the crate, built by hand on the quiet board that the first move left:
            // three reds in row 0 again, one swap away, with (1,0) under the crate.
            Put(board, 0, 0, TileColor.Red);
            Put(board, 1, 0, TileColor.Red);
            Put(board, 2, 0, TileColor.Green);
            Put(board, 3, 0, TileColor.Red);
            Put(board, 0, 1, TileColor.Blue); // keep the neighbors of the new reds from making other runs
            Put(board, 1, 2, TileColor.Purple);
            Put(board, 2, 1, TileColor.Purple);
            Put(board, 3, 1, TileColor.Yellow);
            ResolveResult second = ResolverWithRefill(Cycle(60)).ResolveSwap(board, new GridPos(2, 0), new GridPos(3, 0));

            Assert.That(second.IsValid, Is.True, Describe(board));
            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(second, 1), ObstacleType.Crate, 1, 1), Is.True);
            Assert.That(board.HasCrate(1, 1), Is.False);
        }

        [Test]
        public void Crate_CannotBeSwapped()
        {
            Board board = BoardWithCrateAt11("C");

            ResolveResult withTheTileAbove = ResolverWithRefill().ResolveSwap(board, new GridPos(1, 1), new GridPos(1, 2));
            ResolveResult withTheTileToTheLeft = ResolverWithRefill().ResolveSwap(board, new GridPos(0, 1), new GridPos(1, 1));

            Assert.That(withTheTileAbove.IsValid, Is.False);
            Assert.That(withTheTileToTheLeft.IsValid, Is.False);
            Assert.That(board.HasCrate(1, 1), Is.True);
            Assert.That(board.Get(1, 1), Is.Null);
        }

        // ---------- blasts ----------

        [Test]
        public void RocketBlast_DamagesACrateInItsLine()
        {
            // The horizontal rocket at (0,2) is swapped up to (0,3) and clears the whole top row, where the crate stands at (3,3).
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RPGBY").WithSpecial(0, 2, SpecialType.RocketHorizontal)
                .WithObstacles(
                "...C.",
                ".....",
                ".....",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4, 3).ResolveSwap(board, new GridPos(0, 2), new GridPos(0, 3));

            Assert.That(result.IsValid, Is.True);
            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Crate, 3, 3), Is.True, Describe(board));
        }

        [Test]
        public void RocketBlast_LeavesACrateOutsideItsLineAlone()
        {
            // Same rocket, but the crate is on the row below the one the rocket clears. None of the cleared tiles is
            // a match, so nothing next to the crate counts: only a match or a blast that covers the crate damages it.
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RPGBY").WithSpecial(0, 2, SpecialType.RocketHorizontal)
                .WithObstacles(
                ".....",
                "...C.",
                ".....",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4, 3).ResolveSwap(board, new GridPos(0, 2), new GridPos(0, 3));

            Assert.That(result.IsValid, Is.True);
            Assert.That(StepsOf<ObstacleDestroyedStep>(result, 1), Is.Empty);
            Assert.That(StepsOf<ObstacleDamagedStep>(result, 1), Is.Empty);
        }

        [Test]
        public void BombBlast_DamagesCratesInItsSquare_ButNotOutsideIt()
        {
            // The bomb at (2,2) is swapped up to (2,3). Its 3x3 blast covers x 1..3 and y 2..3 (the top is clipped).
            // The crate at (3,2) is inside it; the crate at (4,1) is outside it.
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRG.Y",
                "BYPR.",
                "RPGBY").WithSpecial(2, 2, SpecialType.Bomb)
                .WithObstacles(
                ".....",
                "...D.",
                "....D",
                ".....");

            ResolveResult result = ResolverWithRefill(0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1).ResolveSwap(board, new GridPos(2, 2), new GridPos(2, 3));

            Assert.That(result.IsValid, Is.True);
            Assert.That(DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 3, 2), Is.Not.Null, "The crate inside the blast took no damage.");
            Assert.That(DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 4, 1), Is.Null, "The crate outside the blast must be untouched.");
        }

        [Test]
        public void ColorBombSwap_DamagesACrateNextToAClearedTile_AndLeavesOtherCratesAlone()
        {
            // The ColorBomb is swapped with a red tile and clears every red tile: (0,0), (3,1) and (3,2).
            // The crate at (2,1) touches the cleared (3,1). The crate at (2,3) touches no red tile.
            Board board = TestBoards.FromRows(
                "GB.PG",
                "PBGRY",
                "BY.RG",
                "*RGBY").WithObstacles(
                "..C..",
                ".....",
                "..C..",
                ".....");

            ResolveResult result = ResolverWithRefill(Cycle(60)).ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.True);
            List<ObstacleDestroyedStep> destroyed = StepsOf<ObstacleDestroyedStep>(result, 1);
            Assert.That(Destroyed(destroyed, ObstacleType.Crate, 2, 1), Is.True, Describe(board));
            Assert.That(Destroyed(destroyed, ObstacleType.Crate, 2, 3), Is.False, "That crate touches no cleared tile.");
        }

        [Test]
        public void WholeBoardClear_HitsEveryCrateOnce()
        {
            // Two ColorBombs swapped together clear the whole board, and every crate is covered by that.
            Board board = TestBoards.FromRows(
                "GBYPG",
                "PBGBY",
                "BYPRG",
                "**GBY").WithObstacles(
                "C...D",
                ".....",
                "..C..",
                ".....");

            ResolveResult result = ResolverWithRefill(
                0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4)
                .ResolveSwap(board, new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(result.IsValid, Is.True);
            List<ObstacleDestroyedStep> destroyed = StepsOf<ObstacleDestroyedStep>(result, 1);
            Assert.That(Destroyed(destroyed, ObstacleType.Crate, 0, 3), Is.True);
            Assert.That(Destroyed(destroyed, ObstacleType.Crate, 2, 1), Is.True);
            ObstacleDamagedStep twoHpCrate = DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 4, 3);
            Assert.That(twoHpCrate, Is.Not.Null);
            Assert.That(twoHpCrate.HpLeft, Is.EqualTo(1), "One wave is one hit, even when the whole board is cleared.");
        }

        private static void Put(Board board, int x, int y, TileColor color)
        {
            board.Set(x, y, board.NewTile(color));
        }
    }
}
