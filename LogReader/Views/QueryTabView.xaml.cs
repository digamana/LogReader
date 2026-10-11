using LogReader.ViewModels;
using System.Windows;

namespace LogReader.Views;

public partial class QueryTabView : System.Windows.Controls.UserControl
{
    public QueryTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is QueryTabViewModel vm)
        {
            vm.RequestFavoriteName = () =>
            {
                var dialog = new AddFavoriteDialog { Owner = Window.GetWindow(this) };
                return dialog.ShowDialog() == true ? dialog.FavoriteName : null;
            };
        }
    }
}
