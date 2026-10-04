using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Finds all matches on a board.
    ///
    /// Idea, in four steps:
    ///   1. Scan every row for "runs": 3 or more same-colored tiles in a row. Give each run an id.
    ///   2. Scan every column the same way.
    ///   3. A tile can be in one horizontal run AND one vertical run (that is an L, T or + shape).
    ///      When that happens, merge the two runs (union-find).
    ///   4. Collect the tiles of each merged group into one Match.
    ///
    /// The finder keeps its working arrays between calls, so calling it repeatedly
    /// (every cascade wave) does not allocate more than the Match objects it returns.
    /// Not thread-safe: use one MatchFinder per owner.
    /// </summary>
    public sealed class MatchFinder
    {
        public const int MinRunLength = 3;

        // For every cell (index = y * width + x): the id of the horizontal / vertical run covering it, or -1.
        private int[] _horizontalRunAt = new int[0];
        private int[] _verticalRunAt = new int[0];

        // Union-find: _parent[runId] points towards the "root" run of its merged group.
        private readonly List<int> _parent = new List<int>();

        // For each root run id: index of its Match in the results list, or -1 if not created yet.
        private readonly List<int> _matchIndexOfRoot = new List<int>();

        /// <summary>
        /// Clears <paramref name="results"/> and fills it with every match on the board.
        /// Empty cells (null) never match. Special tiles match by their color.
        /// </summary>
        public void FindMatches(Board board, List<Match> results)
        {
            results.Clear();
            PrepareBuffers(board);

            ScanRows(board);
            ScanColumns(board);
            MergeCrossingRuns(board);
            BuildMatches(board, results);
        }

        private void PrepareBuffers(Board board)
        {
            int cellCount = board.Width * board.Height;
            if (_horizontalRunAt.Length != cellCount)
            {
                _horizontalRunAt = new int[cellCount];
                _verticalRunAt = new int[cellCount];
            }

            for (int i = 0; i < cellCount; i++)
            {
                _horizontalRunAt[i] = -1;
                _verticalRunAt[i] = -1;
            }

            _parent.Clear();
            _matchIndexOfRoot.Clear();
        }

        private void ScanRows(Board board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int x = 0;
                while (x < board.Width)
                {
                    Tile start = board.Get(x, y);
                    if (start == null)
                    {
                        x++;
                        continue;
                    }

                    // Walk right while the color stays the same. 'end' is the first cell NOT in the run.
                    int end = x + 1;
                    while (end < board.Width)
                    {
                        Tile next = board.Get(end, y);
                        if (next == null || next.Color != start.Color) break;
                        end++;
                    }

                    if (end - x >= MinRunLength)
                    {
                        int runId = CreateRun();
                        for (int i = x; i < end; i++)
                        {
                            _horizontalRunAt[y * board.Width + i] = runId;
                        }
                    }

                    x = end;
                }
            }
        }

        // Same idea as ScanRows, but walking UP a column. The cell index is still y * width + x.
        private void ScanColumns(Board board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int y = 0;
                while (y < board.Height)
                {
                    Tile start = board.Get(x, y);
                    if (start == null)
                    {
                        y++;
                        continue;
                    }

                    int end = y + 1;
                    while (end < board.Height)
                    {
                        Tile next = board.Get(x, end);
                        if (next == null || next.Color != start.Color) break;
                        end++;
                    }

                    if (end - y >= MinRunLength)
                    {
                        int runId = CreateRun();
                        for (int i = y; i < end; i++)
                        {
                            _verticalRunAt[i * board.Width + x] = runId;
                        }
                    }

                    y = end;
                }
            }
        }

        // A cell covered by both a horizontal and a vertical run is where two runs cross.
        // Both runs must end up in the same Match, so we union them.
        private void MergeCrossingRuns(Board board)
        {
            int cellCount = board.Width * board.Height;
            for (int i = 0; i < cellCount; i++)
            {
                int h = _horizontalRunAt[i];
                int v = _verticalRunAt[i];
                if (h >= 0 && v >= 0)
                {
                    Union(h, v);
                }
            }
        }

        private void BuildMatches(Board board, List<Match> results)
        {
            for (int i = 0; i < _parent.Count; i++)
            {
                _matchIndexOfRoot.Add(-1);
            }

            // Visiting cells bottom row first, left to right gives every match a stable, predictable order.
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    int cell = y * board.Width + x;
                    int run = _horizontalRunAt[cell] >= 0 ? _horizontalRunAt[cell] : _verticalRunAt[cell];
                    if (run < 0) continue; // not part of any match

                    int root = Find(run);
                    int matchIndex = _matchIndexOfRoot[root];
                    if (matchIndex < 0)
                    {
                        matchIndex = results.Count;
                        results.Add(new Match(board.Get(x, y).Color));
                        _matchIndexOfRoot[root] = matchIndex;
                    }

                    results[matchIndex].Positions.Add(new GridPos(x, y));
                }
            }
        }

        private int CreateRun()
        {
            int id = _parent.Count;
            _parent.Add(id); // a new run is its own root
            return id;
        }

        private int Find(int run)
        {
            while (_parent[run] != run)
            {
                _parent[run] = _parent[_parent[run]]; // path compression: shortcut towards the root
                run = _parent[run];
            }

            return run;
        }

        private void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA != rootB)
            {
                _parent[rootB] = rootA;
            }
        }
    }
}
