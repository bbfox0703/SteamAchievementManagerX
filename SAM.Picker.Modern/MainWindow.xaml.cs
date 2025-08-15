using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SAM.Picker.Modern
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += async (_, __) => await ViewModel.LoadAsync();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.Filter(SearchBox.Text);
        }

        private void OnLaunchClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is GameViewModel game)
            {
                ViewModel.Launch(game);
            }
        }
    }
}
