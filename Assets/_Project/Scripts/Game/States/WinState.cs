namespace Match3.Game
{
    /// <summary>
    /// The level is won. Input stays off because IdleState switched it off when it exited.
    /// M7 shows the Win screen by listening to GameStateMachine.StateChanged.
    /// </summary>
    public sealed class WinState : IGameState
    {
        public void Enter()
        {
        }

        public void Exit()
        {
        }
    }
}
