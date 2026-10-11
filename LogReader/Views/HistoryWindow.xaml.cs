using LogReader.Models;
using LogReader.Services;
using System.Windows;
using System.Windows.Controls;

namespace LogReader.Views;

public partial class HistoryWindow : Window
{
    private readonly StorageService _storage;
    public QueryHistory? SelectedHistory { get; private set; }

    public HistoryWindow(StorageService storage)
    {
        _storage = storage;
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryListBox.ItemsSource = _storage.LoadHistory();
    }

    private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void UseButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedHistory = HistoryListBox.SelectedItem as QueryHistory;
        if (SelectedHistory == null)
        {
            MessageBox.Show("請先選取一筆紀錄", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var item = HistoryListBox.SelectedItem as QueryHistory;
        if (item == null) return;
        _storage.DeleteHistoryItem(item);
        LoadHistory();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("確定清除全部歷史紀錄？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _storage.ClearHistory();
            LoadHistory();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
