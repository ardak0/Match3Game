using System;
using System.Collections.Generic;
using System.IO;
using Match3.Core;
using Match3.Data;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Tools > Match3 > Difficulty Report: plays every level of the catalog with both bots and writes Docs/difficulty_report.md
    /// (in the project folder, next to the README). It runs in one go with a progress bar and a Cancel button; cancelling writes nothing.
    /// </summary>
    public static class DifficultyReportTool
    {
        private const int RunsPerBot = 400;
        private const int BaseSeed = 1;
        private const string RelativeFile = "Docs/difficulty_report.md";

        [MenuItem("Tools/Match3/Difficulty Report")]
        private static void Run()
        {
            LevelCatalog catalog = LevelCatalogTools.FindCatalog();
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("Difficulty Report", "There is no LevelCatalog asset in the project.", "OK");
                return;
            }

            string problem = catalog.GetProblem();
            if (problem != null)
            {
                EditorUtility.DisplayDialog("Difficulty Report", "Fix the catalog first:\n\n" + problem, "OK");
                return;
            }

            List<LevelReportRow> rows;
            try
            {
                rows = PlayAllLevels(catalog);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (rows == null)
            {
                Debug.Log("Difficulty report cancelled. Nothing was written.");
                return;
            }

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RelativeFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, DifficultyReportBuilder.BuildMarkdown(rows, RunsPerBot, BaseSeed));

            Debug.Log("Difficulty report written to " + path);
            EditorUtility.RevealInFinder(path);
        }

        // Returns one row per level, or null if the user cancelled.
        private static List<LevelReportRow> PlayAllLevels(LevelCatalog catalog)
        {
            List<LevelReportRow> rows = new List<LevelReportRow>();
            int totalRuns = catalog.Count * 2 * RunsPerBot;
            int doneRuns = 0;

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                LevelConfig config = level.ToSimulationConfig();

                SimulationReport random = PlayLevel(config, new RandomBot(), level.name, totalRuns, ref doneRuns);
                if (random == null) return null;

                SimulationReport greedy = PlayLevel(config, new GreedyBot(), level.name, totalRuns, ref doneRuns);
                if (greedy == null) return null;

                rows.Add(LevelReportRow.Create(i + 1, config, level.Stars.TwoStarMovesLeft, level.Stars.ThreeStarMovesLeft, random, greedy));
            }

            return rows;
        }

        // Plays RunsPerBot runs one at a time so the progress bar can move. Null if the user pressed Cancel.
        private static SimulationReport PlayLevel(LevelConfig config, IBotPolicy policy, string levelName, int totalRuns, ref int doneRuns)
        {
            List<RunResult> results = new List<RunResult>(RunsPerBot);

            for (int run = 0; run < RunsPerBot; run++)
            {
                // Updating the bar every 10 runs is plenty and keeps the UI work small.
                if (run % 10 == 0)
                {
                    string info = levelName + ", " + policy.Name + " bot (" + run + "/" + RunsPerBot + ")";
                    if (EditorUtility.DisplayCancelableProgressBar("Difficulty Report", info, (float)doneRuns / totalRuns)) return null;
                }

                results.Add(LevelSimulator.SimulateRun(config, policy, BaseSeed, run));
                doneRuns++;
            }

            return SimulationReport.FromRuns(policy.Name, results);
        }
    }
}
