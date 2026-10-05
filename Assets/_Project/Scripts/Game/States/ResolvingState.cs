using System;
using Match3.Core;

namespace Match3.Game
{
    /// <summary>
    /// Runs right after a valid move has finished animating: counts the cleared tiles towards the goals,
    /// then decides what comes next (win, lose, shuffle a dead board, or wait for the next swipe).
    /// It takes no time itself: it enters, decides, and leaves in the same frame.
    /// </summary>
    public sealed class ResolvingState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly Board _board;
        private readonly MoveFinder _moveFinder;
        private readonly MoveCounter _moves;
        private readonly GoalTracker _goals;
        private ResolveResult _result;

        public ResolvingState(GameStateMachine machine, Board board, MoveFinder moveFinder, MoveCounter moves, GoalTracker goals)
        {
            _machine = machine;
            _board = board;
            _moveFinder = moveFinder;
            _moves = moves;
            _goals = goals;
        }

        /// <summary>Called by SwappingState just before switching to this state.</summary>
        public void SetResult(ResolveResult result)
        {
            _result = result;
        }

        public void Enter()
        {
            if (_result == null) throw new InvalidOperationException("ResolvingState was entered without a result.");

            _goals.Register(_result);
            _result = null;

            switch (LevelRules.GetOutcome(_goals, _moves))
            {
                case LevelOutcome.Won:
                    _machine.ChangeTo<WinState>();
                    return;
                case LevelOutcome.Lost:
                    _machine.ChangeTo<LoseState>();
                    return;
            }

            // The level goes on, but if no swap can make a match the player is stuck: shuffle first.
            if (!_moveFinder.HasPossibleMove(_board))
            {
                _machine.ChangeTo<ShufflingState>();
                return;
            }

            _machine.ChangeTo<IdleState>();
        }

        public void Exit()
        {
        }
    }
}
