using LogReader.Helpers;
using LogReader.Models;
using LogReader.Services;
using Syroot.Windows.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace LogReader.ViewModels;

public class QueryTabViewModel : BaseViewModel
{
    private readonly StorageService _storage;
    private readonly LogSearchService _searchSvc;
    private BackgroundWorker? _searchWorker;
    private BackgroundWorker? _downloadWorker;
    private List<LogEntry> _allResults = new();

    public QueryTabViewModel(StorageService storage, LogSearchService searchSvc, int tabIndex)
    {
        _storage = storage;
        _searchSvc = searchSvc;
        TabName = $"查詢 {tabIndex}";
        StartDate = DateTime.Today;
        EndDate = DateTime.Today;

        Connections = new ObservableCollection<ConnectionProfile>(_storage.LoadConnections());
        Results = new ObservableCollection<LogEntry>();

        BrowsePathCommand = new RelayCommand(BrowsePath);
        StartSearchCommand = new RelayCommand(StartSearch, CanStartSearch);
        CancelSearchCommand = new RelayCommand(CancelSearch, () => IsSearching);
        DownloadRemoteCommand = new RelayCommand(DownloadRemote, CanDownload);
        CancelDownloadCommand = new RelayCommand(CancelDownload, () => IsDownloading);
        ReFilterCommand = new RelayCommand(RunReFilter, CanReFilter);
        ExportCommand = new RelayCommand(ExportToTxt, () => Results.Count > 0);
        AddToFavoritesCommand = new RelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Keywords));
    }

    private string _tabName = string.Empty;
    public string TabName { get => _tabName; set => SetField(ref _tabName, value); }

    private string _localPath = string.Empty;
    public string LocalPath { get => _localPath; set => SetField(ref _localPath, value); }

    private bool _isRemoteMode;
    public bool IsRemoteMode
    {
        get => _isRemoteMode;
        set { SetField(ref _isRemoteMode, value); OnPropertyChanged(nameof(IsLocalMode)); }
    }
    public bool IsLocalMode => !_isRemoteMode;

    public ObservableCollection<ConnectionProfile> Connections { get; }

    private ConnectionProfile? _selectedConnection;
    public ConnectionProfile? SelectedConnection { get => _selectedConnection; set => SetField(ref _selectedConnection, value); }

    private DateTime _startDate;
    public DateTime StartDate { get => _startDate; set => SetField(ref _startDate, value); }

    private DateTime _endDate;
    public DateTime EndDate { get => _endDate; set => SetField(ref _endDate, value); }

    private string _keywords = string.Empty;
    public string Keywords { get => _keywords; set => SetField(ref _keywords, value); }

    private bool _isMatchAll;
    public bool IsMatchAll { get => _isMatchAll; set => SetField(ref _isMatchAll, value); }

    private string _reFilterKeywords = string.Empty;
    public string ReFilterKeywords { get => _reFilterKeywords; set => SetField(ref _reFilterKeywords, value); }

    private bool _isReFilterMatchAll;
    public bool IsReFilterMatchAll { get => _isReFilterMatchAll; set => SetField(ref _isReFilterMatchAll, value); }

    public ObservableCollection<LogEntry> Results { get; }

    private string _statusText = "就緒";
    public string StatusText { get => _statusText; set => SetField(ref _statusText, value); }

    private int _progress;
    public int Progress { get => _progress; set => SetField(ref _progress, value); }

    private string _progressText = string.Empty;
    public string ProgressText { get => _progressText; set => SetField(ref _progressText, value); }

    private bool _isSearching;
    public bool IsSearching
    {
        get => _isSearching;
        set { SetField(ref _isSearching, value); OnPropertyChanged(nameof(IsIdle)); }
    }

    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set { SetField(ref _isDownloading, value); OnPropertyChanged(nameof(IsIdle)); }
    }

    public bool IsIdle => !_isSearching && !_isDownloading;

    private string _downloadedLocalPath = string.Empty;
    public string DownloadedLocalPath { get => _downloadedLocalPath; set => SetField(ref _downloadedLocalPath, value); }

    public ICommand BrowsePathCommand { get; }
    public ICommand StartSearchCommand { get; }
    public ICommand CancelSearchCommand { get; }
    public ICommand DownloadRemoteCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand ReFilterCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand AddToFavoritesCommand { get; }

    // Set by the View to avoid ViewModel referencing View directly
    public Func<string?>? RequestFavoriteName { get; set; }

    private void BrowsePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "選擇 Log 資料夾",
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            LocalPath = dialog.SelectedPath;
    }

    private bool CanStartSearch()
    {
        if (IsSearching || IsDownloading) return false;
        if (IsRemoteMode)
            return !string.IsNullOrWhiteSpace(DownloadedLocalPath) && Directory.Exists(DownloadedLocalPath);
        return !string.IsNullOrWhiteSpace(LocalPath);
    }

    private bool CanDownload()
        => IsRemoteMode && !IsDownloading && !IsSearching && SelectedConnection != null;

    private bool CanReFilter()
        => !IsSearching && _allResults.Count > 0 && !string.IsNullOrWhiteSpace(ReFilterKeywords);

    private List<string> ParseKeywords(string raw)
        => raw.Split('\n', '\r')
              .Select(k => k.Trim())
              .Where(k => !string.IsNullOrEmpty(k))
              .ToList();

    private void StartSearch()
    {
        var keywords = ParseKeywords(Keywords);
        if (keywords.Count == 0 && !string.IsNullOrWhiteSpace(Keywords))
            keywords.Add(Keywords.Trim());

        string searchPath = IsRemoteMode ? DownloadedLocalPath : LocalPath;
        if (!Directory.Exists(searchPath))
        {
            MessageBox.Show("路徑不存在", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var files = _searchSvc.GetLogFiles(searchPath, StartDate, EndDate);
        if (files.Count == 0)
        {
            MessageBox.Show("指定日期範圍內找不到 .log 或 .txt 檔案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Results.Clear();
        _allResults.Clear();
        Progress = 0;
        IsSearching = true;
        StatusText = "查詢中...";

        _searchWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
        _searchWorker.DoWork += (s, e) =>
        {
            var worker = (BackgroundWorker)s!;
            var result = _searchSvc.SearchFiles(files, keywords, IsMatchAll, worker);
            e.Result = result;
        };
        _searchWorker.ProgressChanged += (s, e) =>
        {
            Progress = e.ProgressPercentage;
            ProgressText = e.UserState as string ?? string.Empty;
        };
        _searchWorker.RunWorkerCompleted += (s, e) =>
        {
            IsSearching = false;
            Progress = 100;
            if (e.Error != null) { StatusText = $"錯誤: {e.Error.Message}"; return; }
            if (e.Cancelled) { StatusText = "已取消"; return; }

            var found = e.Result as List<LogEntry> ?? new();
            _allResults = found;
            foreach (var item in found)
                Results.Add(item);

            StatusText = $"找到 {found.Count} 筆結果";
            ProgressText = string.Empty;

            _storage.AddHistory(new QueryHistory
            {
                QueryTime = DateTime.Now,
                SourcePath = searchPath,
                Keywords = Keywords,
                IsMatchAll = IsMatchAll,
                StartDate = StartDate,
                EndDate = EndDate,
                IsRemote = IsRemoteMode,
                ConnectionName = SelectedConnection?.Name ?? string.Empty
            });
        };
        _searchWorker.RunWorkerAsync();
    }

    private void CancelSearch() => _searchWorker?.CancelAsync();

    private void DownloadRemote()
    {
        if (SelectedConnection == null) return;

        Progress = 0;
        IsDownloading = true;
        StatusText = "連線並下載遠端 Log...";
        ProgressText = "連線中...";

        var profile = SelectedConnection;
        var start = StartDate;
        var end = EndDate;

        _downloadWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
        _downloadWorker.DoWork += (s, e) =>
        {
            var worker = (BackgroundWorker)s!;
            using var ssh = new SshFtpService();
            worker.ReportProgress(0, "連線中...");
            if (!ssh.Connect(profile))
                throw new Exception($"無法連線至 {profile.Host}");

            worker.ReportProgress(5, "取得遠端檔案清單...");
            var localDir = ssh.DownloadLogs(profile, start, end, worker);
            e.Result = localDir;
        };
        _downloadWorker.ProgressChanged += (s, e) =>
        {
            Progress = e.ProgressPercentage;
            ProgressText = e.UserState as string ?? string.Empty;
        };
        _downloadWorker.RunWorkerCompleted += (s, e) =>
        {
            IsDownloading = false;
            Progress = 100;
            ProgressText = string.Empty;
            if (e.Error != null)
            {
                StatusText = $"下載失敗: {e.Error.Message}";
                MessageBox.Show(e.Error.Message, "下載失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (e.Cancelled) { StatusText = "下載已取消"; return; }

            DownloadedLocalPath = e.Result as string ?? string.Empty;
            StatusText = $"下載完成 → {DownloadedLocalPath}";
            MessageBox.Show($"Log 已下載至:\n{DownloadedLocalPath}", "下載完成", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        _downloadWorker.RunWorkerAsync();
    }

    private void CancelDownload() => _downloadWorker?.CancelAsync();

    private void RunReFilter()
    {
        var keywords = ParseKeywords(ReFilterKeywords);
        var filtered = _searchSvc.ReFilter(_allResults, keywords, IsReFilterMatchAll);
        Results.Clear();
        foreach (var item in filtered)
            Results.Add(item);
        StatusText = $"二次篩選後：{filtered.Count} 筆結果";
    }

    private void ExportToTxt()
    {
        try
        {
            var downloadsPath = new KnownFolder(KnownFolderType.Downloads).Path;
            var fileName = $"LogExport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(downloadsPath, fileName);

            var lines = Results.Select(r => $"[{r.FileName}:{r.LineNumber}] {r.Content}");
            File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);

            StatusText = $"已匯出至 {filePath}";
            MessageBox.Show($"已匯出至:\n{filePath}", "匯出完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddToFavorites()
    {
        var name = RequestFavoriteName?.Invoke();
        if (string.IsNullOrWhiteSpace(name)) return;

        _storage.AddFavorite(new FavoriteQuery
        {
            Name = name,
            SourcePath = LocalPath,
            Keywords = Keywords,
            IsMatchAll = IsMatchAll,
            StartDate = StartDate,
            EndDate = EndDate,
            IsRemote = IsRemoteMode,
            ConnectionName = SelectedConnection?.Name ?? string.Empty
        });
        StatusText = $"已加入最愛：{name}";
    }

    public void ApplyFavorite(FavoriteQuery fav)
    {
        Keywords = fav.Keywords;
        IsMatchAll = fav.IsMatchAll;
        StartDate = fav.StartDate == default ? DateTime.Today : fav.StartDate;
        EndDate = fav.EndDate == default ? DateTime.Today : fav.EndDate;
        IsRemoteMode = fav.IsRemote;
        if (!fav.IsRemote)
            LocalPath = fav.SourcePath;
        else
        {
            var conn = Connections.FirstOrDefault(c => c.Name == fav.ConnectionName);
            if (conn != null) SelectedConnection = conn;
        }
    }

    public void ApplyHistory(QueryHistory hist)
    {
        Keywords = hist.Keywords;
        IsMatchAll = hist.IsMatchAll;
        StartDate = hist.StartDate;
        EndDate = hist.EndDate;
        IsRemoteMode = hist.IsRemote;
        if (!hist.IsRemote)
            LocalPath = hist.SourcePath;
        else
        {
            var conn = Connections.FirstOrDefault(c => c.Name == hist.ConnectionName);
            if (conn != null) SelectedConnection = conn;
        }
    }

    public void RefreshConnections()
    {
        var list = _storage.LoadConnections();
        Connections.Clear();
        foreach (var c in list) Connections.Add(c);
    }
}
