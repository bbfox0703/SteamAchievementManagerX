using Microsoft.UI.Xaml;

namespace SAM.ModernPicker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        ThemeHelper.Initialize(m_window);
        m_window.Activate();
    }

    private Window? m_window;
}
