using LogReader.Models;
using System.ComponentModel;
using System.IO;

namespace LogReader.Services;

public class LogSearchService
{
    private static readonly string[] AllowedExtensions = { ".txt", ".log" };

    public List<string> GetLogFiles(string folderPath, DateTime startDate, DateTime endDate)
    {
        if (!Directory.Exists(folderPath)) return new List<string>();

        var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

        return Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .Where(f =>
            {
                var created = File.GetCreationTime(f);
                var modified = File.GetLastWriteTime(f);
                var fileDate = created < modified ? created : modified;
                // use the earlier of creation/modification to check against date range
                var latestDate = created > modified ? created : modified;
                return latestDate >= startDate.Date && created <= endOfDay;
            })
            .OrderBy(f => File.GetCreationTime(f))
            .ToList();
    }

    public List<LogEntry> SearchFiles(
        List<string> filePaths,
        List<string> keywords,
        bool isMatchAll,
        BackgroundWorker worker)
    {
        var results = new List<LogEntry>();
        int total = filePaths.Count;
        int processed = 0;

        foreach (var filePath in filePaths)
        {
            if (worker.CancellationPending) break;

            try
            {
                var lines = File.ReadAllLines(filePath);
                var fileName = Path.GetFileName(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool matches = isMatchAll
                        ? keywords.All(k => line.Contains(k, StringComparison.OrdinalIgnoreCase))
                        : keywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase));

                    if (matches)
                    {
                        results.Add(new LogEntry
                        {
                            FileName = fileName,
                            LineNumber = i + 1,
                            Content = line,
                            FilePath = filePath
                        });
                    }
                }
            }
            catch { /* skip unreadable files */ }

            processed++;
            int pct = (int)((double)processed / total * 100);
            worker.ReportProgress(pct, $"分析中 {processed}/{total}: {Path.GetFileName(filePath)}");
        }

        return results;
    }

    public List<LogEntry> ReFilter(
        List<LogEntry> source,
        List<string> keywords,
        bool isMatchAll)
    {
        if (keywords.Count == 0) return source;

        return source.Where(e =>
            isMatchAll
                ? keywords.All(k => e.Content.Contains(k, StringComparison.OrdinalIgnoreCase))
                : keywords.Any(k => e.Content.Contains(k, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }
}
