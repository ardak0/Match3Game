using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// Plays a level many times with a bot and reports how hard it is. This is the balancing tool: instead of guessing
    /// how many moves a level needs, run it a few hundred times and read the win rate.
    ///
    /// A run copies what LevelController does: same board generation, same resolver, same shuffle when no move is left,
    /// same goal counting. Only the player is a bot.
    ///
    /// DETERMINISM: nothing here uses the clock, a global random, or a Guid. Run number i of a simulation with base seed S
    /// gets two seeds that are computed from (S, i): one for the board (generation, refills, shuffles) and one for the bot.
    /// The same level, bot, runs and base seed therefore always give the same report. The two random streams are separate,
    /// so a bot that uses more or less randomness never changes the boards it plays on.
    /// </summary>
    public static class LevelSimulator
    {
        private const int BoardStream = 1;
        private const int BotStream = 2;

        /// <summary>Plays the level "runs" times and summarizes. Runs with index 0, 1, 2, ... use seeds from baseSeed.</summary>
        public static SimulationReport Simulate(LevelConfig level, IBotPolicy policy, int runs, int baseSeed)
        {
            List<RunResult> results = new List<RunResult>(runs);
            for (int i = 0; i < runs; i++) results.Add(SimulateRun(level, policy, baseSeed, i));

            return SimulationReport.FromRuns(policy.Name, results);
        }

        /// <summary>
        /// One play-through. Public so a caller (the Level Editor) can play the runs one at a time,
        /// show a progress bar between them and stop when the user cancels.
        /// </summary>
        public static RunResult SimulateRun(LevelConfig level, IBotPolicy policy, int baseSeed, int runIndex)
        {
            TileColor[] colors = level.GetColors();

            if (!ObstacleLayout.TryParse(level.ObstacleRows, level.Width, level.Height, out ObstacleLayout layout, out string error))
            {
                throw new ArgumentException("Obstacle layout " + error, nameof(level));
            }

            SystemRandom boardRandom = new SystemRandom(MakeSeed(baseSeed, runIndex, BoardStream));
            SystemRandom botRandom = new SystemRandom(MakeSeed(baseSeed, runIndex, BotStream));

            Board board = new BoardGenerator(boardRandom).Generate(level.Width, level.Height, colors, layout);
            BoardResolver resolver = new BoardResolver(boardRandom, colors);
            MoveFinder moveFinder = new MoveFinder(new MatchFinder());
            BoardShuffler shuffler = new BoardShuffler(boardRandom, moveFinder);
            GoalTracker goals = new GoalTracker(level.Goals);
            List<SwapMove> moves = new List<SwapMove>();

            policy.StartRun(colors, botRandom);

            int movesLeft = level.MoveLimit;
            while (true)
            {
                if (goals.AllGoalsMet) return RunResult.Won(movesLeft); // goals first: finishing on the last move is a win
                if (movesLeft == 0) return RunResult.Lost(GetProgress(goals, level.Goals));

                moveFinder.GetAllMoves(board, moves);
                if (moves.Count == 0)
                {
                    // Like the real game: a board with no move is shuffled, and if that fails the level ends.
                    if (!shuffler.TryShuffle(board, out _)) return RunResult.Lost(GetProgress(goals, level.Goals), shuffleFailed: true);
                    moveFinder.GetAllMoves(board, moves);
                }

                SwapMove move = policy.ChooseMove(board, moves, goals);
                ResolveResult result = resolver.ResolveSwap(board, move.A, move.B);
                if (!result.IsValid) throw new InvalidOperationException("The bot chose a move that is not valid: " + move);

                goals.Register(result);
                movesLeft--;
            }
        }

        // Share of all goal counts that is done: 0 at the start, 1 when every goal is met.
        private static double GetProgress(GoalTracker goals, IReadOnlyList<GoalDefinition> definitions)
        {
            double total = 0;
            double done = 0;

            for (int i = 0; i < definitions.Count; i++)
            {
                total += definitions[i].count;
                done += definitions[i].count - goals.GetRemaining(i);
            }

            return done / total;
        }

        // Mixes the three numbers into one seed (a splitmix-style scramble), so neighbouring run numbers
        // get seeds that look unrelated. unchecked: overflow is wanted here.
        private static int MakeSeed(int baseSeed, int runIndex, int stream)
        {
            unchecked
            {
                ulong x = (ulong)(uint)baseSeed * 0x9E3779B97F4A7C15UL;
                x += (ulong)(uint)runIndex * 0xBF58476D1CE4E5B9UL;
                x += (ulong)(uint)stream * 0x94D049BB133111EBUL;
                x ^= x >> 30;
                x *= 0xBF58476D1CE4E5B9UL;
                x ^= x >> 27;
                x *= 0x94D049BB133111EBUL;
                x ^= x >> 31;
                return (int)(x & 0x7FFFFFFF);
            }
        }
    }
}
