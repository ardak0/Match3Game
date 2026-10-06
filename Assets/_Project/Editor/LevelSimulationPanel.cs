using System;
using System.Collections.Generic;
using System.Text;
using Match3.Core;
using Match3.Data;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// The "Balance simulation" part of the Level Editor: pick a bot and a number of runs, press Simulate, read the report.
    /// The runs are played a few at a time from EditorApplication.update, so the window keeps responding, the progress bar moves
    /// and Cancel works. There are no threads: one tick plays runs for about 40 ms and then returns to Unity.
    /// The level is copied into a LevelConfig when the simulation starts, so editing the level while it runs changes nothing.
    /// </summary>
    public sealed class LevelSimulationPanel
    {
        private const double SliceSeconds = 0.04;
        private const int DefaultRuns = 200;
        private const int BaseSeed = 1; // the same seed every time, so the same level always gives the same report

        private static readonly string[] PolicyNames = { "Greedy (rough skilled player)", "Random (lower bound)" };

        private readonly Action _repaint;

        private int _policyIndex;
        private int _runs = DefaultRuns;

        // the running simulation
        private bool _running;
        private bool _cancelRequested;
        private LevelConfig _config;
        private IBotPolicy _policy;
        private List<RunResult> _results;
        private int _nextRun;
        private int _totalRuns;

        // the last finished report
        private SimulationReport _report;
        private string _reportSignature;
        private string _error;

        public LevelSimulationPanel(Action repaint)
        {
            _repaint = repaint;
        }

        public bool IsRunning => _running;

        /// <summary>Stops a running simulation without keeping its numbers, and forgets the shown report (used when the window moves to another level).</summary>
        public void Reset()
        {
            Stop();
            _report = null;
            _error = null;
        }

        /// <summary>Call from the window's OnDisable, so the update callback never outlives the window.</summary>
        public void Stop()
        {
            if (_running) EditorApplication.update -= Step;
            _running = false;
            _cancelRequested = false;
        }

        /// <param name="levelHasProblems">Simulating a broken level makes no sense (and an invalid layout throws), so the button is off.</param>
        /// <param name="applyStars">Called with (two-star, three-star) when the designer presses "Use these".</param>
        public void Draw(LevelData level, bool levelHasProblems, Action<int, int> applyStars)
        {
            EditorGUILayout.LabelField("Balance simulation", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_running))
            {
                _policyIndex = EditorGUILayout.Popup("Bot", _policyIndex, PolicyNames);
                _runs = Mathf.Clamp(EditorGUILayout.IntField("Runs", _runs), 10, 5000);
            }

            if (_running)
            {
                Rect bar = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(bar, (float)_nextRun / _totalRuns, _nextRun + " / " + _totalRuns + " runs");
                if (GUILayout.Button("Cancel")) _cancelRequested = true;
                return;
            }

            using (new EditorGUI.DisabledScope(levelHasProblems))
            {
                if (GUILayout.Button("Simulate")) Start(level);
            }

            if (levelHasProblems) EditorGUILayout.LabelField("Fix the problems above first.", EditorStyles.miniLabel);

            if (_error != null) EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (_report != null) DrawReport(level, applyStars);
        }

        // ---------- running ----------

        private void Start(LevelData level)
        {
            _error = null;
            _report = null;
            _config = level.ToSimulationConfig();
            _reportSignature = GetSignature(_config);
            _policy = _policyIndex == 0 ? (IBotPolicy)new GreedyBot() : new RandomBot();
            _results = new List<RunResult>(_runs);
            _totalRuns = _runs;
            _nextRun = 0;
            _cancelRequested = false;
            _running = true;

            EditorApplication.update += Step;
        }

        private void Step()
        {
            try
            {
                double end = EditorApplication.timeSinceStartup + SliceSeconds;
                do
                {
                    if (_cancelRequested || _nextRun >= _totalRuns) break;

                    _results.Add(LevelSimulator.SimulateRun(_config, _policy, BaseSeed, _nextRun));
                    _nextRun++;
                }
                while (EditorApplication.timeSinceStartup < end);
            }
            catch (Exception exception)
            {
                Stop();
                _error = "The simulation failed: " + exception.Message;
                _repaint();
                return;
            }

            if (_cancelRequested || _nextRun >= _totalRuns) Finish();
            _repaint();
        }

        private void Finish()
        {
            _report = SimulationReport.FromRuns(_policy.Name, _results, _cancelRequested);
            Stop();
        }

        // ---------- showing the report ----------

        private void DrawReport(LevelData level, Action<int, int> applyStars)
        {
            EditorGUILayout.Space(4);

            bool outdated = GetSignature(level.ToSimulationConfig()) != _reportSignature;
            string title = _report.PolicyName + " bot, " + _report.Runs + " runs";
            if (_report.Cancelled) title += " (cancelled)";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (outdated)
            {
                EditorGUILayout.HelpBox("The level was changed after this simulation. Press Simulate again for current numbers.", MessageType.Warning);
            }

            EditorGUILayout.LabelField("Win rate", DifficultyReportBuilder.Percent(_report.WinRate) + "   (" + _report.Wins + " of " + _report.Runs + ")");
            EditorGUILayout.LabelField("Average moves left on a win", _report.HasWins ? _report.AverageMovesLeftOnWin.ToString("0.0") : "-");
            EditorGUILayout.LabelField("Moves left on a win, 40th / 75th percentile", _report.HasWins ? _report.MovesLeftP40 + " / " + _report.MovesLeftP75 : "-");
            EditorGUILayout.LabelField("Average goal progress on a loss", _report.Losses > 0 ? DifficultyReportBuilder.Percent(_report.AverageGoalProgressOnLoss) : "-");

            if (_report.ShuffleFailures > 0)
            {
                EditorGUILayout.HelpBox(_report.ShuffleFailures + " run(s) ended because the board had no move and could not be shuffled.", MessageType.Error);
            }

            if (_report.PolicyName != "Greedy")
            {
                EditorGUILayout.LabelField("The difficulty label and the star suggestion come from the Greedy bot. Run that one too.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            DrawDifficulty();
            DrawStarSuggestion(level, applyStars, outdated);
        }

        private void DrawDifficulty()
        {
            DifficultyRating rating = DifficultyRater.FromGreedyWinRate(_report.WinRate);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = GetRatingColor(rating);

            EditorGUILayout.LabelField("Difficulty: " + DifficultyRater.GetLabel(rating) + "  (greedy win rate " + DifficultyReportBuilder.Percent(_report.WinRate) + ")", style);
        }

        private void DrawStarSuggestion(LevelData level, Action<int, int> applyStars, bool outdated)
        {
            if (!StarSuggester.TrySuggest(_report, level.MoveLimit, out int two, out int three))
            {
                EditorGUILayout.LabelField("No star suggestion: the greedy bot never won (or the move limit is below 3).", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            DrawStarNumbers(level, two, three);

            using (new EditorGUI.DisabledScope(outdated))
            {
                if (GUILayout.Button("Use these star numbers")) applyStars(two, three);
            }
        }

        private static void DrawStarNumbers(LevelData level, int two, int three)
        {
            EditorGUILayout.LabelField("Suggested stars (moves left)", "2 stars: " + two + "     3 stars: " + three
                + "     (now " + level.Stars.TwoStarMovesLeft + " / " + level.Stars.ThreeStarMovesLeft + ")");
        }

        private static Color GetRatingColor(DifficultyRating rating)
        {
            switch (rating)
            {
                case DifficultyRating.Easy: return new Color(0.2f, 0.7f, 0.3f);
                case DifficultyRating.Medium: return new Color(0.8f, 0.7f, 0.1f);
                case DifficultyRating.Hard: return new Color(0.95f, 0.5f, 0.1f);
                default: return new Color(0.9f, 0.2f, 0.2f);
            }
        }

        // Everything about a level that changes how it plays (the stars do not, so they are left out).
        private static string GetSignature(LevelConfig config)
        {
            StringBuilder text = new StringBuilder();
            text.Append(config.Width).Append('x').Append(config.Height).Append(' ').Append(config.ColorCount).Append(' ').Append(config.MoveLimit);

            for (int i = 0; i < config.Goals.Count; i++)
            {
                GoalDefinition goal = config.Goals[i];
                text.Append('|').Append((int)goal.kind).Append(',').Append((int)goal.color).Append(',').Append((int)goal.obstacle).Append(',').Append(goal.count);
            }

            if (config.ObstacleRows != null)
            {
                for (int i = 0; i < config.ObstacleRows.Count; i++) text.Append('/').Append(config.ObstacleRows[i]);
            }

            return text.ToString();
        }
    }
}
