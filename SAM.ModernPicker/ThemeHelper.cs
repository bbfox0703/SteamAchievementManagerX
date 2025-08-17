using Microsoft.UI.Xaml;

namespace SAM.ModernPicker;

public static class ThemeHelper
{
    private static Window? _window;

    public static void Initialize(Window window)
    {
        _window = window;
    }

    public static void Apply(ElementTheme theme)
    {
        if (_window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }

    public static void ApplyCustom()
    {
        Apply(ElementTheme.Default);
    }
}
