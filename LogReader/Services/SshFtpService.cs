using LogReader.Models;
using Renci.SshNet;
using System.ComponentModel;
using System.IO;

namespace LogReader.Services;

public class SshFtpService : IDisposable
{
    private SftpClient? _sftpClient;
    private static readonly string[] AllowedExtensions = { ".txt", ".log" };

    public bool Connect(ConnectionProfile profile)
    {
        try
        {
            _sftpClient = new SftpClient(profile.Host, profile.SshPort, profile.Username, profile.Password);
            _sftpClient.Connect();
            return _sftpClient.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    public bool IsConnected => _sftpClient?.IsConnected ?? false;

    public List<string> ListRemoteLogFiles(string remotePath, DateTime startDate, DateTime endDate)
    {
        if (_sftpClient == null || !_sftpClient.IsConnected)
            throw new InvalidOperationException("未連線");

        var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

        try
        {
            return _sftpClient.ListDirectory(remotePath)
                .Where(f => !f.IsDirectory)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f.Name).ToLower()))
                .Where(f => f.LastWriteTime >= startDate.Date && f.LastWriteTime <= endOfDay)
                .Select(f => f.FullName)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public string DownloadLogs(
        ConnectionProfile profile,
        DateTime startDate,
        DateTime endDate,
        BackgroundWorker worker)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeName = string.IsNullOrEmpty(profile.Name)
            ? profile.Host.Replace(".", "_")
            : profile.Name.Replace(" ", "_");
        var localDir = Path.Combine(@"C:\temp\LogReader", $"{safeName}_{timestamp}");

        if (!Directory.Exists(@"C:\temp"))
            Directory.CreateDirectory(@"C:\temp");
        if (!Directory.Exists(@"C:\temp\LogReader"))
            Directory.CreateDirectory(@"C:\temp\LogReader");
        Directory.CreateDirectory(localDir);

        var files = ListRemoteLogFiles(profile.RemoteLogPath, startDate, endDate);
        int total = files.Count;
        int done = 0;

        foreach (var remoteFile in files)
        {
            if (worker.CancellationPending) break;

            var localPath = Path.Combine(localDir, Path.GetFileName(remoteFile));
            try
            {
                using var fs = File.OpenWrite(localPath);
                _sftpClient!.DownloadFile(remoteFile, fs);
            }
            catch { /* skip failed files */ }

            done++;
            int pct = (int)((double)done / total * 100);
            worker.ReportProgress(pct, $"下載 {done}/{total}: {Path.GetFileName(remoteFile)}");
        }

        return localDir;
    }

    public void Disconnect()
    {
        _sftpClient?.Disconnect();
    }

    public void Dispose()
    {
        Disconnect();
        _sftpClient?.Dispose();
        _sftpClient = null;
    }
}
