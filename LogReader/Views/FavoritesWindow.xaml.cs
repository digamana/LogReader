using LogReader.Models;
using LogReader.Services;
using System.Windows;
using System.Windows.Controls;

namespace LogReader.Views;

public partial class FavoritesWindow : Window
{
    private readonly StorageService _storage;
    public FavoriteQuery? SelectedFavorite { get; private set; }

    public FavoritesWindow(StorageService storage)
    {
        _storage = storage;
        InitializeComponent();
        LoadFavorites();
    }

    private void LoadFavorites()
    {
        FavListBox.ItemsSource = _storage.LoadFavorites();
    }

    private void FavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedFavorite = FavListBox.SelectedItem as FavoriteQuery;
        if (SelectedFavorite == null)
        {
            MessageBox.Show("請先選取一筆最愛", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var item = FavListBox.SelectedItem as FavoriteQuery;
        if (item == null) return;
        _storage.DeleteFavorite(item);
        LoadFavorites();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
