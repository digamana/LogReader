using System.Windows;

namespace LogReader.Views;

public partial class AddFavoriteDialog : Window
{
    public string FavoriteName { get; private set; } = string.Empty;

    public AddFavoriteDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("請輸入名稱", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        FavoriteName = NameTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
