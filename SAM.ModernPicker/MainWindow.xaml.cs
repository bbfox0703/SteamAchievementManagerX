using Microsoft.UI.Xaml;

namespace SAM.ModernPicker;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    private void OnLightTheme(object sender, RoutedEventArgs e) => ThemeHelper.Apply(ElementTheme.Light);
    private void OnDarkTheme(object sender, RoutedEventArgs e) => ThemeHelper.Apply(ElementTheme.Dark);
    private void OnCustomTheme(object sender, RoutedEventArgs e) => ThemeHelper.ApplyCustom();
}
