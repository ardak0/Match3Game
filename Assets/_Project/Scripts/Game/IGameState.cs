namespace Match3.Game
{
    /// <summary>
    /// One phase of the game loop (waiting for a swipe, animating a move, level won...).
    /// The state machine calls Exit on the old state, then Enter on the new one.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Exit();
    }
}
