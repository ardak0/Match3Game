using System;

namespace Match3.Game
{
    /// <summary>
    /// Counts the moves the player has left. Plain C#: other code listens to MovesChanged instead of polling.
    /// </summary>
    public sealed class MoveCounter
    {
        public MoveCounter(int moveLimit)
        {
            if (moveLimit < 1) throw new ArgumentOutOfRangeException(nameof(moveLimit), "A level needs at least one move.");

            MoveLimit = moveLimit;
            MovesLeft = moveLimit;
        }

        public int MoveLimit { get; }
        public int MovesLeft { get; private set; }
        public bool HasMovesLeft => MovesLeft > 0;

        /// <summary>Raised with the new MovesLeft after every spent move.</summary>
        public event Action<int> MovesChanged;

        /// <summary>Spends one move. Only call it for a valid swap. The idle state guarantees a move is left.</summary>
        public void UseMove()
        {
            if (MovesLeft == 0) throw new InvalidOperationException("No moves left.");

            MovesLeft--;
            MovesChanged?.Invoke(MovesLeft);
        }
    }
}
