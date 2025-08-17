using Microsoft.UI.Xaml;
using Microsoft.Windows.AppSDK;
using System;
using System.Runtime.InteropServices;

namespace ModernSAM.UI;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Bootstrap.Initialize();
            Application.Start(p => new App());
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is COMException)
        {
            Console.Error.WriteLine("ModernSAM.UI requires the Windows App SDK runtime and can only run on Windows.");
        }
        finally
        {
            // Ensure the Windows App SDK is cleaned up if initialization succeeded
            Bootstrap.Shutdown();
        }
    }
}
