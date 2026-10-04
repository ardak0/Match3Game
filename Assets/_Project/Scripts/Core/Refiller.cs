using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Fills every empty cell with a new random tile.
    /// Run it AFTER gravity: then the empty cells are the top of each column,
    /// and the new tiles can drop in from above the board.
    /// </summary>
    public sealed class Refiller
    {
        private readonly IRandom _random;
        private readonly IReadOnlyList<TileColor> _colors;
        private readonly List<TileSpawn> _spawns = new List<TileSpawn>();

        public Refiller(IRandom random, IReadOnlyList<TileColor> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                throw new ArgumentException("Need at least one color to refill with.", nameof(colors));
            }

            _random = random;
            _colors = colors;
        }

        /// <summary>
        /// Fills all empty cells and returns what was created, or null if the board was already full.
        /// Cells are filled column by column (left to right), each column from the bottom up,
        /// so the same seed always gives the same tiles.
        /// New tiles may form matches: that is how cascades start.
        /// </summary>
        public SpawnStep Refill(Board board, int wave)
        {
            _spawns.Clear();

            for (int x = 0; x < board.Width; x++)
            {
                int spawnedInColumn = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.Get(x, y) != null) continue;

                    TileColor color = _colors[_random.Next(0, _colors.Count)];
                    Tile tile = board.NewTile(color);
                    board.Set(x, y, tile);

                    _spawns.Add(new TileSpawn(
                        tile.Id, tile.Color, tile.Special, new GridPos(x, y), board.Height + spawnedInColumn));
                    spawnedInColumn++;
                }
            }

            if (_spawns.Count == 0) return null;

            return new SpawnStep(wave, _spawns.ToArray());
        }
    }
}
