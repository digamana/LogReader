using LogReader.Models;
using LogReader.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogReader.Views;

public partial class ConnectionManagerWindow : Window
{
    private readonly StorageService _storage;
    private List<ConnectionProfile> _connections = new();

    public ConnectionManagerWindow(StorageService storage)
    {
        _storage = storage;
        InitializeComponent();
        LoadConnections();
        UpdateFileStatus();
    }

    private void LoadConnections()
    {
        _connections = _storage.LoadConnections();
        RefreshList();
        EditPanel.IsEnabled = false;
    }

    private void UpdateFileStatus()
    {
        if (_storage.ConnectionsFileExists())
        {
            FileStatusText.Text = $"✓ 目前使用外部檔案：{StorageService.ConnectionsFilePath}";
            FileStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16));
            ExportButton.Content = "🔄 重新匯出 Connections.json";
        }
        else
        {
            FileStatusText.Text = "⚠ 目前使用 EXE 內嵌設定，尚未產生外部檔案";
            FileStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 100, 0));
            ExportButton.Content = "📄 產生 Connections.json";
        }
    }

    private void ConnectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var profile = ConnectionListBox.SelectedItem as ConnectionProfile;
        EditPanel.IsEnabled = profile != null;
        if (profile == null) return;

        NameBox.Text = profile.Name;
        TypeBox.Text = profile.Type;
        ProjNameBox.Text = profile.ProjName;
        MemoBox.Text = profile.Memo;
        HostBox.Text = profile.Host;
        SshPortBox.Text = profile.SshPort.ToString();
        UsernameBox.Text = profile.Username;
        PasswordBox.Password = profile.Password;
        RemotePathBox.Text = profile.RemoteLogPath;
    }

    private void FieldChanged(object sender, TextChangedEventArgs e) { }
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) { }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new ConnectionProfile
        {
            Name = $"新連線 {_connections.Count + 1}",
            Type = "Test",
            SshPort = 22,
            RemoteLogPath = "/var/log/"
        };
        _connections.Add(newProfile);
        _storage.SaveConnections(_connections);
        RefreshList();
        ConnectionListBox.SelectedItem = newProfile;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = ConnectionListBox.SelectedItem as ConnectionProfile;
        if (profile == null) return;

        var copy = new ConnectionProfile
        {
            Name = $"{profile.Name} - 複製",
            Type = profile.Type,
            ProjName = profile.ProjName,
            Memo = profile.Memo,
            Host = profile.Host,
            SshPort = profile.SshPort,
            Username = profile.Username,
            Password = profile.Password,
            RemoteLogPath = profile.RemoteLogPath
        };
        _connections.Add(copy);
        _storage.SaveConnections(_connections);
        RefreshList();
        ConnectionListBox.SelectedItem = copy;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = ConnectionListBox.SelectedItem as ConnectionProfile;
        if (profile == null) return;

        var result = MessageBox.Show($"確定刪除「{profile.Name}」？", "確認",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _connections.Remove(profile);
        _storage.SaveConnections(_connections);
        RefreshList();

        if (_connections.Count > 0)
            ConnectionListBox.SelectedItem = _connections[^1];
        else
            EditPanel.IsEnabled = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = ConnectionListBox.SelectedItem as ConnectionProfile;
        if (profile == null) return;

        if (!int.TryParse(SshPortBox.Text, out int port)) port = 22;

        profile.Name = NameBox.Text.Trim();
        profile.Type = TypeBox.Text.Trim();
        profile.ProjName = ProjNameBox.Text.Trim();
        profile.Memo = MemoBox.Text.Trim();
        profile.Host = HostBox.Text.Trim();
        profile.SshPort = port;
        profile.Username = UsernameBox.Text.Trim();
        profile.Password = PasswordBox.Password;
        profile.RemoteLogPath = RemotePathBox.Text.Trim();

        _storage.SaveConnections(_connections);
        RefreshList();

        if (_storage.ConnectionsFileExists())
            MessageBox.Show("已儲存至 Connections.json", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("已暫存（僅限本次執行）。\n若要永久保存，請點「產生 Connections.json」。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        _storage.ExportConnections(_connections);
        UpdateFileStatus();
        MessageBox.Show($"已匯出至：\n{StorageService.ConnectionsFilePath}\n\n往後儲存操作將自動同步至此檔案。",
            "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshList()
    {
        var selected = ConnectionListBox.SelectedItem;
        ConnectionListBox.ItemsSource = null;
        ConnectionListBox.ItemsSource = _connections;
        ConnectionListBox.DisplayMemberPath = nameof(ConnectionProfile.Name);
        if (selected != null && _connections.Contains(selected))
            ConnectionListBox.SelectedItem = selected;
    }
}
