using Microsoft.UI.Xaml;

namespace SAM.Picker.Modern
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
