using System.Windows;

namespace SAM.Game
{
    public partial class ManagerWindow : Window
    {
        public ManagerWindow()
        {
            InitializeComponent();
        }

        private void OnStore(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Store clicked";
            StatusText.Visibility = Visibility.Visible;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Data refreshed";
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
