namespace LogReader.Models;

public class FavoriteQuery
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public bool IsMatchAll { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRemote { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
}
