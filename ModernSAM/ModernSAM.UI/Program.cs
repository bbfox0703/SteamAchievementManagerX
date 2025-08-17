using Microsoft.UI.Xaml;

namespace ModernSAM.UI;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.Start(p => new App());
    }
}
