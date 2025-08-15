using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SAM.Game.Core;

namespace SAM.Game.Cross;

public partial class App : Application
{
    private readonly GameService _service;

    public App(GameService service)
    {
        _service = service;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow(_service);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
