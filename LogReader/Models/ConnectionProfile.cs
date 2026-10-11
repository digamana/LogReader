namespace LogReader.Models;

public class ConnectionProfile
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Test";
    public string ProjName { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int SshPort { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemoteLogPath { get; set; } = "/var/log/";

    public override string ToString() => string.IsNullOrEmpty(Name) ? Host : $"{Name} ({Host})";
}
