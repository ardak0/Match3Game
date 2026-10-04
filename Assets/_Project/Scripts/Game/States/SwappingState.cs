using System;
using Match3.Core;
using Match3.View;

namespace Match3.Game
{
    /// <summary>
    /// Handles one swipe: the model resolves it instantly, then the view plays it.
    ///
    ///   invalid swap -> the tiles bounce back, no move is spent, back to Idle.
    ///   valid swap   -> one move is spent, the whole cascade is animated, then ResolvingState.
    /// </summary>
    public sealed class SwappingState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly Board _board;
        private readonly BoardResolver _resolver;
        private readonly StepPlayer _stepPlayer;
        private readonly MoveCounter _moves;
        private readonly ResolvingState _resolving;
        private readonly Action _onPlaybackFinished;

        private GridPos _a;
        private GridPos _b;
        private bool _swapWasValid;

        public SwappingState(
            GameStateMachine machine,
            Board board,
            BoardResolver resolver,
            StepPlayer stepPlayer,
            MoveCounter moves,
            ResolvingState resolving)
        {
            _machine = machine;
            _board = board;
            _resolver = resolver;
            _stepPlayer = stepPlayer;
            _moves = moves;
            _resolving = resolving;
            _onPlaybackFinished = OnPlaybackFinished;
        }

        /// <summary>Called by IdleState just before switching to this state.</summary>
        public void SetSwipe(GridPos a, GridPos b)
        {
            _a = a;
            _b = b;
        }

        public void Enter()
        {
            // Read the ids first: for a rejected swap the view needs them to animate the bounce.
            Tile tileA = _board.Get(_a);
            Tile tileB = _board.Get(_b);

            ResolveResult result = _resolver.ResolveSwap(_board, _a, _b); // the model decides, instantly
            _swapWasValid = result.IsValid;

            if (_swapWasValid)
            {
                _moves.UseMove();
                _resolving.SetResult(result); // ResolvingState counts goals once the animation is over
                _stepPlayer.Play(result.Steps, _onPlaybackFinished);
            }
            else
            {
                _stepPlayer.PlayRejectedSwap(tileA.Id, tileB.Id, _a, _b, _onPlaybackFinished);
            }
        }

        public void Exit()
        {
        }

        private void OnPlaybackFinished()
        {
            if (_swapWasValid) _machine.ChangeTo<ResolvingState>();
            else _machine.ChangeTo<IdleState>();
        }
    }
}
