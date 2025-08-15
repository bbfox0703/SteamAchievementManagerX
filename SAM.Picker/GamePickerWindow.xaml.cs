using System.Windows;

namespace SAM.Picker
{
    public partial class GamePickerWindow : Window
    {
        public GamePickerWindow()
        {
            InitializeComponent();
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Games refreshed";
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
