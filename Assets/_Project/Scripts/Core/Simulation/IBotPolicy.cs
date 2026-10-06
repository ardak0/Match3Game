using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// A way of choosing the next swap. The simulator asks the policy once per move.
    /// StartRun is called at the beginning of every run, so a policy never carries anything over from one run to the next.
    /// </summary>
    public interface IBotPolicy
    {
        string Name { get; }

        /// <summary>
        /// Called before the first move of a run. The colors are the level's colors; the random is this run's bot random
        /// (separate from the board's random), the only source of randomness a policy may use.
        /// </summary>
        void StartRun(IReadOnlyList<TileColor> colors, IRandom random);

        /// <summary>
        /// Picks one of the valid moves (the list is never empty). The board is the real one: a policy must not change it.
        /// The goals show what is still to do.
        /// </summary>
        SwapMove ChooseMove(Board board, IReadOnlyList<SwapMove> validMoves, GoalTracker goals);
    }
}
