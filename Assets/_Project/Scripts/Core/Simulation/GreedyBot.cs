using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Tries every valid move on a COPY of the board and plays the one that advances the goals the most
    /// (tiles of a goal color cleared; for an obstacle goal every hit on that obstacle type counts, whether it breaks the obstacle
    /// or only cracks it, so the bot works on a 2 HP layer before the first one is gone; progress beyond what a goal still needs
    /// does not count for tiles and breaks).
    /// If several moves are equally good, one of them is picked with the run's bot random.
    ///
    /// It is a rough model of a skilled player: it sees the whole board and always takes the best immediate result,
    /// but it does not plan ahead and does not save special tiles. Random is the lower bound and greedy a rough upper bound,
    /// so a real player should land in between.
    ///
    /// The preview of a move uses its own throwaway random for the refill. The real move later gets the real refill,
    /// so the bot cannot see which new tiles will fall in, just like a person cannot. What is cleared by the swap itself
    /// is the same in the preview and in the real move; only the cascades after it can differ.
    /// </summary>
    public sealed class GreedyBot : IBotPolicy
    {
        private IRandom _random;
        private BoardResolver _preview;
        private readonly List<int> _bestMoves = new List<int>();

        public string Name => "Greedy";

        public void StartRun(IReadOnlyList<TileColor> colors, IRandom random)
        {
            _random = random;

            // The preview resolver gets a seed taken from the bot random, so the whole run stays reproducible.
            _preview = new BoardResolver(new SystemRandom(random.Next(0, int.MaxValue)), colors);
        }

        public SwapMove ChooseMove(Board board, IReadOnlyList<SwapMove> validMoves, GoalTracker goals)
        {
            int bestScore = -1;
            _bestMoves.Clear();

            for (int i = 0; i < validMoves.Count; i++)
            {
                Board copy = board.Clone();
                ResolveResult result = _preview.ResolveSwap(copy, validMoves[i].A, validMoves[i].B);

                int score = Score(result, goals);
                if (score > bestScore)
                {
                    bestScore = score;
                    _bestMoves.Clear();
                }

                if (score == bestScore) _bestMoves.Add(i);
            }

            return validMoves[_bestMoves[_random.Next(0, _bestMoves.Count)]];
        }

        // How much of what the goals still need this move would do.
        private static int Score(ResolveResult result, GoalTracker goals)
        {
            int score = 0;

            for (int i = 0; i < goals.GoalCount; i++)
            {
                int remaining = goals.GetRemaining(i);
                if (remaining == 0) continue;

                if (goals.GetKind(i) == GoalKind.ClearObstacle)
                {
                    ObstacleType type = goals.GetObstacle(i);
                    score += Math.Min(result.GetDestroyedCount(type), remaining) + result.GetDamagedCount(type);
                }
                else
                {
                    score += Math.Min(result.GetClearedCount(goals.GetColor(i)), remaining);
                }
            }

            return score;
        }
    }
}
