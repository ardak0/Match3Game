using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// The board had no possible move, so the tiles were rearranged. Every tile that ended up in a
    /// different cell is listed in Moves (tiles that stayed put are not listed).
    /// Unlike FallStep, a shuffled tile can move in any direction, not only down.
    /// This step always has wave 0: it is not part of a swap's cascade.
    /// </summary>
    public sealed class ShuffleStep : ResolveStep
    {
        public IReadOnlyList<TileMove> Moves { get; }

        public ShuffleStep(IReadOnlyList<TileMove> moves) : base(0)
        {
            Moves = moves;
        }
    }
}
