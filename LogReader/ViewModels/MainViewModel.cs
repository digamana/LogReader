using LogReader.Helpers;
using LogReader.Models;
using LogReader.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LogReader.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly StorageService _storage;
    private readonly LogSearchService _searchSvc;
    private int _tabCounter = 1;

    public MainViewModel()
    {
        _storage = new StorageService();
        _searchSvc = new LogSearchService();

        Tabs = new ObservableCollection<QueryTabViewModel>();
        AddTabCommand = new RelayCommand(AddTab);
        CloseTabCommand = new RelayCommand<QueryTabViewModel>(CloseTab);
        OpenConnectionManagerCommand = new RelayCommand(OpenConnectionManager);
        OpenHistoryCommand = new RelayCommand(OpenHistory);
        OpenFavoritesCommand = new RelayCommand(OpenFavorites);

        AddTab();
    }

    public ObservableCollection<QueryTabViewModel> Tabs { get; }

    private QueryTabViewModel? _selectedTab;
    public QueryTabViewModel? SelectedTab { get => _selectedTab; set => SetField(ref _selectedTab, value); }

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand OpenConnectionManagerCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand OpenFavoritesCommand { get; }

    public void AddTab()
    {
        var vm = new QueryTabViewModel(_storage, _searchSvc, _tabCounter++);
        Tabs.Add(vm);
        SelectedTab = vm;
    }

    private void CloseTab(QueryTabViewModel? tab)
    {
        if (tab == null || Tabs.Count <= 1) return;
        var idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        SelectedTab = Tabs[Math.Max(0, idx - 1)];
    }

    private void OpenConnectionManager()
    {
        var win = new Views.ConnectionManagerWindow(_storage);
        win.ShowDialog();
        foreach (var tab in Tabs)
            tab.RefreshConnections();
    }

    private void OpenHistory()
    {
        var win = new Views.HistoryWindow(_storage);
        win.ShowDialog();
        if (win.SelectedHistory != null)
            SelectedTab?.ApplyHistory(win.SelectedHistory);
    }

    private void OpenFavorites()
    {
        var win = new Views.FavoritesWindow(_storage);
        win.ShowDialog();
        if (win.SelectedFavorite != null)
            SelectedTab?.ApplyFavorite(win.SelectedFavorite);
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter is T t ? t : default);
    public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
}
