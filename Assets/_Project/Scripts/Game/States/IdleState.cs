using System;
using Match3.Core;
using Match3.View;

namespace Match3.Game
{
    /// <summary>
    /// Waiting for the player. This is the only state in which swipes are accepted:
    /// it listens to the swipe event (and enables input) on Enter, and stops on Exit.
    /// </summary>
    public sealed class IdleState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly SwipeInput _swipeInput;
        private readonly SwappingState _swapping;
        private readonly Action<GridPos, GridPos> _onSwipe;

        public IdleState(GameStateMachine machine, SwipeInput swipeInput, SwappingState swapping)
        {
            _machine = machine;
            _swipeInput = swipeInput;
            _swapping = swapping;
            _onSwipe = OnSwipe; // created once, so subscribing does not allocate every time
        }

        public void Enter()
        {
            _swipeInput.SwipeDetected += _onSwipe;
            _swipeInput.InputEnabled = true;
        }

        public void Exit()
        {
            _swipeInput.SwipeDetected -= _onSwipe;
            _swipeInput.InputEnabled = false;
        }

        private void OnSwipe(GridPos a, GridPos b)
        {
            _swapping.SetSwipe(a, b); // hand the swipe over, then switch state
            _machine.ChangeTo<SwappingState>();
        }
    }
}
