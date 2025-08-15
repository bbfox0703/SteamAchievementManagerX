using System;
using Avalonia;
using SAM.Game.Core;

namespace SAM.Game.Cross;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0], out long appId))
        {
            Console.Error.WriteLine("Application ID argument is required.");
            return;
        }

        using var client = new API.Client();
        try
        {
            client.Initialize(appId);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Failed to initialize Steam client: {e.Message}");
            return;
        }

        var service = new GameService(appId, client);
        BuildAvaloniaApp(service).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(GameService service) =>
        AppBuilder.Configure(() => new App(service))
            .UsePlatformDetect()
            .LogToTrace();
}
