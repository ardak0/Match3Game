namespace Match3.Game
{
    /// <summary>
    /// The player ran out of moves with goals left. Input stays off because IdleState switched it off when it exited.
    /// M7 shows the Lose screen by listening to GameStateMachine.StateChanged.
    /// </summary>
    public sealed class LoseState : IGameState
    {
        public void Enter()
        {
        }

        public void Exit()
        {
        }
    }
}
