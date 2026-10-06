using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Match3.Core
{
    /// <summary>
    /// Writes the difficulty report as a Markdown table, one row per level. Pure text building: the editor tool
    /// collects the rows and writes the file. The text has no date or time, so running the report twice with the same
    /// settings gives a byte-identical file (and a clean diff in version control).
    /// </summary>
    public static class DifficultyReportBuilder
    {
        public static string BuildMarkdown(IReadOnlyList<LevelReportRow> rows, int runsPerBot, int baseSeed)
        {
            StringBuilder text = new StringBuilder();
            text.Append("# Difficulty report\n\n");
            text.Append("Made by Tools > Match3 > Difficulty Report. Every level was played ").Append(runsPerBot)
                .Append(" times by each bot (base seed ").Append(baseSeed).Append(").\n\n");
            text.Append("- **Random win**: win rate of a bot that plays any valid move. A lower bound.\n");
            text.Append("- **Greedy win**: win rate of a bot that plays the move that advances the goals most. A rough skilled player.\n");
            text.Append("- **Avg moves left**: average moves left on a win, greedy bot.\n");
            text.Append("- **Rating** (greedy win rate): Easy above 80%, Medium 50-80%, Hard 20-50%, Very Hard below 20%.\n");
            text.Append("- **Stars** are written as 2-star/3-star moves left. Suggested = 40th and 75th percentile of the greedy wins.\n\n");

            text.Append("| Level | Size | Colors | Moves | Goals | Obstacles | Random win | Greedy win | Avg moves left | Rating | Stars now | Stars suggested |\n");
            text.Append("|---|---|---|---|---|---|---|---|---|---|---|---|\n");

            for (int i = 0; i < rows.Count; i++) AppendRow(text, rows[i]);

            return text.ToString();
        }

        private static void AppendRow(StringBuilder text, LevelReportRow row)
        {
            string suggested = row.HasSuggestion ? row.SuggestedTwoStar + "/" + row.SuggestedThreeStar : "n/a";

            text.Append("| ").Append(row.LevelNumber);
            text.Append(" | ").Append(row.Width).Append('x').Append(row.Height);
            text.Append(" | ").Append(row.ColorCount);
            text.Append(" | ").Append(row.MoveLimit);
            text.Append(" | ").Append(row.GoalsText);
            text.Append(" | ").Append(row.ObstaclesText);
            text.Append(" | ").Append(Percent(row.Random.WinRate));
            text.Append(" | ").Append(Percent(row.Greedy.WinRate));
            text.Append(" | ").Append(row.Greedy.HasWins ? row.Greedy.AverageMovesLeftOnWin.ToString("0.0", CultureInfo.InvariantCulture) : "n/a");
            text.Append(" | ").Append(DifficultyRater.GetLabel(row.Rating));
            text.Append(" | ").Append(row.CurrentTwoStar).Append('/').Append(row.CurrentThreeStar);
            text.Append(" | ").Append(suggested);
            text.Append(" |\n");
        }

        /// <summary>0.294 becomes "29%".</summary>
        public static string Percent(double rate)
        {
            return (rate * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
