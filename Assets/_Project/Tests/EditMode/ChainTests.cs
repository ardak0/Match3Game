using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using static Match3.Tests.ObstacleTestHelpers;

namespace Match3.Tests
{
    public class ChainTests
    {
        // The same quiet 5x4 board as the other obstacle tests: swapping (2,0) and (3,0) clears the red match (0,0)-(1,0)-(2,0).
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
        public void ChainedTile_CannotBeSwapped_AndTheBoardStaysAsItWas()
        {
            // The red tile at (3,0) is chained. The swap that would make a match with it is refused.
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                "...L.");
            Tile chainedTile = board.Get(3, 0);
            Tile otherTile = board.Get(2, 0);

            ResolveResult result = ResolverWithRefill().ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(result.IsValid, Is.False, "A swap with a chained tile is not a move, so no move is used.");
            Assert.That(result.Steps, Is.Empty);
            Assert.That(board.Get(3, 0), Is.SameAs(chainedTile));
            Assert.That(board.Get(2, 0), Is.SameAs(otherTile));
            Assert.That(board.IsChained(3, 0), Is.True);
        }

        [Test]
        public void ChainedTile_CannotBeSwapped_FromEitherSide()
        {
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                "..L..");

            ResolveResult result = ResolverWithRefill().ResolveSwap(board, MatchSwapB, MatchSwapA); // the chained tile is the second one now

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void ChainedTile_InAMatch_BreaksItsChain_AndStaysOnTheBoard()
        {
            // The chained red tile at (1,0) is part of the match (0,0)-(1,0)-(2,0). Only its chain breaks: the other two are cleared.
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                ".L...");
            Tile chainedTile = board.Get(1, 0);

            ResolveResult result = ResolverWithRefill(3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(result.IsValid, Is.True);
            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Chain, 1, 0), Is.True);
            Assert.That(IsCleared(StepsOf<ClearStep>(result, 1)[0], chainedTile.Id), Is.False, "The tile itself must not be cleared.");
            Assert.That(StepsOf<ClearStep>(result, 1)[0].Tiles.Count, Is.EqualTo(2));
            Assert.That(board.Get(1, 0), Is.SameAs(chainedTile));
            Assert.That(board.IsChained(1, 0), Is.False);
            Assert.That(result.GetClearedCount(TileColor.Red), Is.EqualTo(2), "The chained tile is not counted as cleared.");
        }

        [Test]
        public void ChainedTile_AfterItsChainBroke_CanBeSwapped()
        {
            Board board = QuietBoard(
                ".....",
                ".....",
                ".....",
                ".L...");
            ResolverWithRefill(3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);
            Assert.That(board.IsChained(1, 0), Is.False, "Precondition: the chain broke.");
            Tile freedTile = board.Get(1, 0);

            // A second move built by hand around the freed red tile at (1,0): swapping it up with the yellow tile at (1,1)
            // puts it between two red tiles in row 1.
            Rewrite(board, 0, 0,
                "PBG",
                "RYR",
                "G.B");
            Assert.That(board.Get(1, 0), Is.SameAs(freedTile));

            ResolveResult second = ResolverWithRefill(3, 4, 3, 4, 3, 4, 3, 4, 3, 4).ResolveSwap(board, new GridPos(1, 0), new GridPos(1, 1));

            Assert.That(second.IsValid, Is.True, "A freed tile moves like any other.\n" + Describe(board));
        }

        [Test]
        public void ChainedTile_HitByARocket_OnlyLosesItsChain()
        {
            // The rocket clears the top row. The tile at (3,3) is chained: it stays and only the chain breaks.
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RPGBY").WithSpecial(0, 2, SpecialType.RocketHorizontal)
                .WithObstacles(
                "...L.",
                ".....",
                ".....",
                ".....");
            Tile chainedTile = board.Get(3, 3);

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, new GridPos(0, 2), new GridPos(0, 3));

            Assert.That(Destroyed(StepsOf<ObstacleDestroyedStep>(result, 1), ObstacleType.Chain, 3, 3), Is.True);
            Assert.That(IsCleared(StepsOf<ClearStep>(result, 1)[0], chainedTile.Id), Is.False);
            Assert.That(board.Get(3, 3), Is.SameAs(chainedTile));
        }

        [Test]
        public void ChainedSpecial_InAMatch_DoesNotGoOff()
        {
            // The middle of the match is a chained rocket. Its chain breaks, so it stays and does not clear its row.
            Board board = TestBoards.FromRows(
                "GBYPR",
                "PRGBY",
                "BYPRG",
                "RRGRP").WithSpecial(1, 0, SpecialType.RocketHorizontal)
                .WithObstacles(
                ".....",
                ".....",
                ".....",
                ".L...");
            Tile rocket = board.Get(1, 0);

            ResolveResult result = ResolverWithRefill(3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(StepsOf<ClearStep>(result, 1)[0].Tiles.Count, Is.EqualTo(2), "Only the two plain tiles of the match clear.");
            Assert.That(board.Get(1, 0), Is.SameAs(rocket));
            Assert.That(board.Get(1, 0).Special, Is.EqualTo(SpecialType.RocketHorizontal));
        }

        [Test]
        public void ChainedTile_DoesNotFall_AndBlocksTheTilesAboveIt()
        {
            // The chained tile at (1,1) hangs over the gap at (1,0). The tiles above it stay on top of it.
            Board board = QuietBoard(
                ".....",
                ".....",
                ".L...",
                ".....");
            Tile chainedTile = board.Get(1, 1);
            Tile above = board.Get(1, 2);
            Tile aboveThat = board.Get(1, 3);

            ResolveResult result = ResolverWithRefill(3, 4, 3, 4).ResolveSwap(board, MatchSwapA, MatchSwapB);

            Assert.That(board.Get(1, 1), Is.SameAs(chainedTile), "A chained tile never falls.\n" + Describe(board));
            Assert.That(board.IsChained(1, 1), Is.True);
            Assert.That(board.Get(1, 2), Is.SameAs(above));
            Assert.That(board.Get(1, 3), Is.SameAs(aboveThat));
            Assert.That(board.Get(1, 0), Is.Not.Null, "The gap under the chained tile is filled from the side.");
        }
    }
}
