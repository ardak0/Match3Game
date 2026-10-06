using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    public class IceTests
    {
        // The same quiet 5x4 board as the crate tests: swapping (2,0) and (3,0) clears the red match (0,0)-(1,0)-(2,0).
        private static Board QuietBoard(params string[] obstacleRowsTopFirst)
        {
            return TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RRGRP").WithObstacles(obstacleRowsTopFirst);
        }

        private static readonly GridPos MatchSwapA = new GridPos(2, 0);
        private static readonly GridPos MatchSwapB = new GridPos(3, 0);

        [Test]
        public void Ice_UnderAClearedTile_WithOneHp_IsDestroyed()
        {
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                ".I...");

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Ice, 1, 0), Is.True);
            Assert.That(board.GetObstacle(1, 0).IsNone, Is.True);
            Assert.That(board.Get(1, 0), Is.Not.Null, "The cell is filled again; only the ice is gone.");
        }

        [Test]
        public void Ice_WithTwoHp_LosesOneHpPerClear_AndStaysInItsCell()
        {
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                ".J...");

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            ObstacleDamagedStep damaged = DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 1, 0);
            Assert.That(damaged, Is.Not.Null);
            Assert.That(damaged.Type, Is.EqualTo(ObstacleType.Ice));
            Assert.That(damaged.HpLeft, Is.EqualTo(1));
            Assert.That(board.GetObstacle(1, 0), Is.EqualTo(new Obstacle(ObstacleType.Ice, 1)));
            Assert.That(board.Get(1, 0), Is.Not.Null, "A new tile now lies on the cracked ice.");
        }

        [Test]
        public void Ice_UnderATileThatIsNotCleared_IsUntouched()
        {
            Board board = QuietBoard(
                "....J",
                ".....",
                ".....",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(StepsOf<ObstacleDamagedStep>(result, 1), Is.Empty);
            Assert.That(board.GetObstacle(4, 3), Is.EqualTo(new Obstacle(ObstacleType.Ice, 2)));
        }

        [Test]
        public void Ice_IsNotMovedByGravity_AndTheTileOnItFallsAwayNormally()
        {
            // The ice is at (1,1), under the yellow tile. The tile below it, at (1,0), is cleared by the match,
            // so the yellow tile falls down one cell. The ice must stay at (1,1).
            Board board = QuietBoard(
                ".....",
                ".....",
                ".J...",
                ".....");
            Tile yellow = board.Get(1, 1);

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            List<FallStep> falls = StepsOf<FallStep>(result, 1);
            bool yellowFell = false;
            for (int i = 0; i < falls.Count; i++)
            {
                for (int j = 0; j < falls[i].Moves.Count; j++)
                {
                    TileMove move = falls[i].Moves[j];
                    if (move.TileId == yellow.Id && move.From == new GridPos(1, 1) && move.To == new GridPos(1, 0)) yellowFell = true;
                }
            }

            Assert.That(yellowFell, Is.True, "The tile on the ice should fall like any other tile.\n" + Describe(board));
            Assert.That(board.Get(1, 0), Is.SameAs(yellow));
            Assert.That(board.GetObstacle(1, 1), Is.EqualTo(new Obstacle(ObstacleType.Ice, 2)), "The ice must stay where it was.");
            Assert.That(board.GetObstacle(1, 0).IsNone, Is.True, "The ice must not move down with the tile.");
        }

        [Test]
        public void Ice_InTheSwappedCells_StaysInItsCell()
        {
            // The match swap exchanges the tiles at (2,0) and (3,0). The red tile that moves to (2,0) is cleared there,
            // which breaks the ice at (2,0). The green tile that moves to (3,0) is not cleared, so the ice at (3,0) is untouched.
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                "..II.");

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(result.IsValid, Is.True);
            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Ice, 2, 0), Is.True);
            Assert.That(board.GetObstacle(3, 0), Is.EqualTo(new Obstacle(ObstacleType.Ice, 1)));
        }

        [Test]
        public void Ice_UnderATileHitByARocket_TakesTheHit()
        {
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RPGBY").WithSpecial(0, 2, SpecialType.RocketHorizontal)
                .WithObstacles(
                "...J.",
                ".....",
                ".....",
                ".....");

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4, 3).ResolveSwap(board, new GridPos(0, 2), new GridPos(0, 3));

            Assert.That(DamagedAt(StepsOf<ObstacleDamagedStep>(result, 1), 3, 3), Is.Not.Null, "The ice under a tile cleared by the rocket took no hit.");
        }

        [Test]
        public void Ice_DoesNotStopTilesFromMatching()
        {
            // The whole bottom row lies on ice and the match still happens as if the ice were not there.
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                "IIIII");

            ResolveResult result = ResolverWithRefill(3, 4, 3).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.GetClearedCount(TileColor.Red), Is.GreaterThanOrEqualTo(3));
        }
    }
}
