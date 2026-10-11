using LogReader.ViewModels;
using System.Windows;

namespace LogReader;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
