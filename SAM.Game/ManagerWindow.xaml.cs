using ModernWpf.Controls;
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
            StatusBar.Title = "Store";
            StatusBar.Message = "Store clicked";
            StatusBar.IsOpen = true;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            StatusBar.Title = "Refresh";
            StatusBar.Message = "Data refreshed";
            StatusBar.IsOpen = true;
        }
    }
}
