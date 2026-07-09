using System.Collections.Generic;
using System.Text;

/// One row of the Results/History screen: an experiment's best attempt.
public struct ResultRow
{
    public string moduleId;
    public string title;
    public ExperimentPeriod period;
    public bool attempted;
    public bool passed;
    public float bestGrade;    // 0..100
    public float bestMastery;  // 0..1
    public int attempts;
}

/// The manuscript's analytics dashboard, descoped (plan §S2) to a local
/// Results/History view + an exportable scores file. Pure C# over ProgressionService
/// + ExperimentCatalog so it is unit-testable and drives both the screen and the export.
public static class ResultsExport
{
    /// Per-experiment rows in catalog order (unattempted experiments show as blanks).
    public static List<ResultRow> BuildRows(ProgressionService service)
    {
        var rows = new List<ResultRow>();
        if (service == null) return rows;
        foreach (var e in ExperimentCatalog.Entries)
        {
            var rec = service.GetRecord(e.moduleId);
            rows.Add(new ResultRow
            {
                moduleId = e.moduleId,
                title = e.title,
                period = e.period,
                attempted = rec != null,
                passed = rec != null && rec.passed,
                bestGrade = rec != null ? rec.bestGrade : 0f,
                bestMastery = rec != null ? rec.bestMastery : 0f,
                attempts = rec != null ? rec.attempts : 0,
            });
        }
        return rows;
    }

    /// Count of passed experiments over the whole roster.
    public static int PassedCount(ProgressionService service)
    {
        int n = 0;
        foreach (var r in BuildRows(service)) if (r.passed) n++;
        return n;
    }

    /// A spreadsheet-ready export of the whole transcript.
    public static string BuildCsv(ProgressionService service)
    {
        var sb = new StringBuilder();
        sb.Append("Experiment,Period,Attempted,Passed,Best Grade %,Best Mastery %,Attempts\n");
        foreach (var r in BuildRows(service))
        {
            sb.Append(Escape(r.title)).Append(',')
              .Append(r.period).Append(',')
              .Append(r.attempted ? "yes" : "no").Append(',')
              .Append(r.passed ? "yes" : "no").Append(',')
              .Append(Round(r.bestGrade)).Append(',')
              .Append(Round(r.bestMastery * 100f)).Append(',')
              .Append(r.attempts).Append('\n');
        }
        int passed = PassedCount(service);
        sb.Append("TOTAL,,,").Append(passed).Append('/').Append(ExperimentCatalog.Count)
          .Append(",,,\n");
        return sb.ToString();
    }

    private static int Round(float v) => UnityEngine.Mathf.RoundToInt(v);

    // CSV-safe: quote fields containing a comma or quote.
    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOf(',') < 0 && s.IndexOf('"') < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
