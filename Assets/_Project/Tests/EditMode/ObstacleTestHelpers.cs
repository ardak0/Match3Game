using System.Collections.Generic;
using Match3.Core;

namespace Match3.Tests
{
    /// <summary>Small helpers shared by the obstacle tests (crates, ice, chains, diagonal gravity).</summary>
    public static class ObstacleTestHelpers
    {
        /// <summary>The five colors the obstacle tests use: Red=0 Green=1 Blue=2 Yellow=3 Purple=4.</summary>
        public static readonly TileColor[] Colors =
        {
            TileColor.Red, TileColor.Green, TileColor.Blue, TileColor.Yellow, TileColor.Purple
        };

        /// <summary>A resolver whose refill gives exactly these colors (as numbers into Colors), in the order Core asks for them.</summary>
        public static BoardResolver ResolverWithRefill(params int[] refillColors)
        {
            return new BoardResolver(new ScriptedRandom(refillColors), Colors);
        }

        /// <summary>
        /// A long refill script that repeats the colors 0,1,2,3,4. For tests that only look at the first wave: whatever cascades
        /// follow, ScriptedRandom never runs dry.
        /// </summary>
        public static int[] Cycle(int length)
        {
            int[] values = new int[length];
            for (int i = 0; i < length; i++) values[i] = i % Colors.Length;
            return values;
        }

        /// <summary>All steps of one type that belong to one wave (wave 1 = the clear that the player's swap caused).</summary>
        public static List<T> StepsOf<T>(ResolveResult result, int wave) where T : ResolveStep
        {
            List<T> found = new List<T>();
            for (int i = 0; i < result.Steps.Count; i++)
            {
                if (result.Steps[i] is T step && step.Wave == wave) found.Add(step);
            }

            return found;
        }

        /// <summary>All steps of one type, from every wave.</summary>
        public static List<T> AllStepsOf<T>(ResolveResult result) where T : ResolveStep
        {
            List<T> found = new List<T>();
            for (int i = 0; i < result.Steps.Count; i++)
            {
                if (result.Steps[i] is T step) found.Add(step);
            }

            return found;
        }

        /// <summary>True if a destroyed step of this type at this cell is in the list.</summary>
        public static bool Destroyed(List<ObstacleDestroyedStep> steps, ObstacleType type, int x, int y)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Type == type && steps[i].Position == new GridPos(x, y)) return true;
            }

            return false;
        }

        /// <summary>The damaged step for this cell, or null.</summary>
        public static ObstacleDamagedStep DamagedAt(List<ObstacleDamagedStep> steps, int x, int y)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Position == new GridPos(x, y)) return steps[i];
            }

            return null;
        }

        /// <summary>True if the tile with this id is in the clear step.</summary>
        public static bool IsCleared(ClearStep clear, int tileId)
        {
            for (int i = 0; i < clear.Tiles.Count; i++)
            {
                if (clear.Tiles[i].TileId == tileId) return true;
            }

            return false;
        }

        /// <summary>
        /// Overwrites a rectangle of tiles with new ones, so a test can set up a second move on the board the first move left behind.
        /// Rows are written top row first; x0 and y0 are the cell of the BOTTOM-LEFT letter. A '.' leaves that cell as it is
        /// (use it for crate cells). Obstacles are not touched.
        /// </summary>
        public static void Rewrite(Board board, int x0, int y0, params string[] rowsTopFirst)
        {
            for (int row = 0; row < rowsTopFirst.Length; row++)
            {
                int y = y0 + rowsTopFirst.Length - 1 - row;
                for (int column = 0; column < rowsTopFirst[row].Length; column++)
                {
                    char letter = rowsTopFirst[row][column];
                    if (letter == '.') continue;
                    board.Set(x0 + column, y, board.NewTile(ColorOf(letter)));
                }
            }
        }

        private static TileColor ColorOf(char letter)
        {
            switch (letter)
            {
                case 'R': return TileColor.Red;
                case 'G': return TileColor.Green;
                case 'B': return TileColor.Blue;
                case 'Y': return TileColor.Yellow;
                case 'P': return TileColor.Purple;
                default: return TileColor.Orange;
            }
        }

        /// <summary>
        /// The steps of a result as one line of text (types, tile ids, cells, rounds). Two resolves that did exactly the same
        /// give the same text, so tests can compare "the same seed gives the same result".
        /// </summary>
        public static string Signature(ResolveResult result)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            for (int i = 0; i < result.Steps.Count; i++)
            {
                ResolveStep step = result.Steps[i];
                text.Append('w').Append(step.Wave).Append(':');
                if (step is ClearStep clear)
                {
                    text.Append("clear");
                    for (int j = 0; j < clear.Tiles.Count; j++) text.Append(' ').Append(clear.Tiles[j].TileId);
                }
                else if (step is FallStep fall)
                {
                    text.Append("fall r").Append(fall.Round);
                    for (int j = 0; j < fall.Moves.Count; j++)
                    {
                        text.Append(' ').Append(fall.Moves[j].TileId).Append(fall.Moves[j].From).Append(fall.Moves[j].To);
                    }
                }
                else if (step is SpawnStep spawn)
                {
                    text.Append("spawn r").Append(spawn.Round);
                    for (int j = 0; j < spawn.Spawns.Count; j++)
                    {
                        text.Append(' ').Append(spawn.Spawns[j].TileId).Append(spawn.Spawns[j].Color).Append(spawn.Spawns[j].To);
                    }
                }
                else if (step is ObstacleDamagedStep damaged)
                {
                    text.Append("damaged ").Append(damaged.Type).Append(damaged.Position).Append(damaged.HpLeft);
                }
                else if (step is ObstacleDestroyedStep destroyed)
                {
                    text.Append("destroyed ").Append(destroyed.Type).Append(destroyed.Position);
                }
                else
                {
                    text.Append(step.GetType().Name);
                }

                text.Append(" | ");
            }

            return text.ToString();
        }

        /// <summary>The board as text, top row first, for failure messages. Letters are tiles, 'x' crates, '.' holes; '~' marks ice and '#' a chain.</summary>
        public static string Describe(Board board)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            for (int y = board.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Tile tile = board.Get(x, y);
                    text.Append(board.HasCrate(x, y) ? 'x' : tile == null ? '.' : "RGBYPO*"[(int)tile.Color]);
                    Obstacle obstacle = board.GetObstacle(x, y);
                    text.Append(obstacle.Type == ObstacleType.Ice ? '~' : obstacle.Type == ObstacleType.Chain ? '#' : ' ');
                }

                text.Append('\n');
            }

            return text.ToString();
        }
    }
}
