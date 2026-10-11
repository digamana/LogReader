namespace LogReader.Models;

public class LogEntry
{
    public string FileName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
