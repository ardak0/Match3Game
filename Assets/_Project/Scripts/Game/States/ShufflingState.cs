using Match3.Core;
using Match3.View;

namespace Match3.Game
{
    /// <summary>
    /// Entered when the board is stable but no swap can make a match. The model shuffles the tiles instantly,
    /// the view glides them to their new cells, then the game waits for the next swipe.
    /// If no valid shuffle can be found (very unlikely), the level is lost instead of leaving the player stuck.
    /// </summary>
    public sealed class ShufflingState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly Board _board;
        private readonly BoardShuffler _shuffler;
        private readonly StepPlayer _stepPlayer;
        private readonly System.Action _onPlaybackFinished;

        public ShufflingState(GameStateMachine machine, Board board, BoardShuffler shuffler, StepPlayer stepPlayer)
        {
            _machine = machine;
            _board = board;
            _shuffler = shuffler;
            _stepPlayer = stepPlayer;
            _onPlaybackFinished = OnPlaybackFinished;
        }

        public void Enter()
        {
            if (_shuffler.TryShuffle(_board, out ShuffleStep step))
            {
                _stepPlayer.PlayShuffle(step, _onPlaybackFinished);
            }
            else
            {
                _machine.ChangeTo<LoseState>();
            }
        }

        public void Exit()
        {
        }

        private void OnPlaybackFinished()
        {
            _machine.ChangeTo<IdleState>();
        }
    }
}
