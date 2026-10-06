using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Plays any valid move, chosen at random. It never looks at the goals, so it is the WORST sensible player:
    /// its win rate is a lower bound. Together with GreedyBot it gives the range a real person will land in.
    /// </summary>
    public sealed class RandomBot : IBotPolicy
    {
        private IRandom _random;

        public string Name => "Random";

        public void StartRun(IReadOnlyList<TileColor> colors, IRandom random)
        {
            _random = random;
        }

        public SwapMove ChooseMove(Board board, IReadOnlyList<SwapMove> validMoves, GoalTracker goals)
        {
            return validMoves[_random.Next(0, validMoves.Count)];
        }
    }
}
