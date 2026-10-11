namespace LogReader.Models;

public class QueryHistory
{
    public DateTime QueryTime { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public bool IsMatchAll { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRemote { get; set; }
    public string ConnectionName { get; set; } = string.Empty;

    public string DisplayText =>
        $"[{QueryTime:MM/dd HH:mm}] {(IsRemote ? $"遠端:{ConnectionName}" : SourcePath)} | {(IsMatchAll ? "全部" : "任一")}:{Keywords.Replace("\n", ";")}";
}
